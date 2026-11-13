// Horse Racing - a simple example of coordinating threads with AutoResetEvents.
//
// The MAIN thread is the "race official": it controls the timing.
// Each HORSE is its own worker thread.
//
// One turn of the race looks like this:
//   1. Main signals goHorse[i] for every horse      ("Horse i, take your turn!")
//   2. Each horse wakes up, maybe takes 1 step, then signals goMain[i]  ("Done!")
//   3. Main waits until ALL horses have signaled goMain[i]
//   4. Only then does main check for winners and draw the screen.

// ---------------- Race settings ----------------
const int NumHorses = 4;
const int RaceLength = 50;

// Stage 1: false -> every horse steps every turn (NumHorses-way tie!)
// Stage 2: true  -> each horse has a 1 in 100 chance of stepping each turn
bool UseRandomSteps = false;

// Milliseconds to pause between turns (random mode needs many more turns, so go faster)
int turnDelay = UseRandomSteps ? 5 : 100;

// ---------------- Shared data ----------------
int[] position = new int[NumHorses];   // how far each horse has run
bool raceOver = false;                 // main sets this when there is a winner

// One "goHorse" event per horse: main -> horse  (your turn)
// One "goMain"  event per horse: horse -> main  (I'm done with my turn)
AutoResetEvent[] goHorse = new AutoResetEvent[NumHorses];
AutoResetEvent[] goMain  = new AutoResetEvent[NumHorses];

for (int i = 0; i < NumHorses; i++)
{
    goHorse[i] = new AutoResetEvent(false);
    goMain[i]  = new AutoResetEvent(false);
}

// ---------------- Start the horse threads ----------------
Thread[] horses = new Thread[NumHorses];
for (int i = 0; i < NumHorses; i++)
{
    int id = i; // copy the loop variable so each thread gets its own number
    horses[i] = new Thread(() => RunHorse(id));
    horses[i].Start();
}

// ---------------- Main thread: the race official ----------------
List<int> winners = new List<int>();
int turn = 0;

while (!raceOver)
{
    turn++;

    // 1. Tell every horse it may take its turn
    for (int i = 0; i < NumHorses; i++)
        goHorse[i].Set();

    // 2. Wait for EVERY horse to report back
    for (int i = 0; i < NumHorses; i++)
        goMain[i].WaitOne();

    // 3. All horses are done - now it's safe to check for winners...
    for (int i = 0; i < NumHorses; i++)
    {
        if (position[i] >= RaceLength)
            winners.Add(i);
    }
    if (winners.Count > 0)
        raceOver = true;

    // 4. ...and draw the screen
    DrawScreen();

    Thread.Sleep(turnDelay);
}

// Race is over: wake each horse one last time so it sees raceOver and quits
for (int i = 0; i < NumHorses; i++)
    goHorse[i].Set();

foreach (Thread h in horses)
    h.Join();

Console.WriteLine("All horses are back in the stable. Goodbye!");


// ---------------- Worker thread: one horse ----------------
void RunHorse(int id)
{
    while (true)
    {
        // Wait until main says it's my turn
        goHorse[id].WaitOne();

        if (raceOver)
            break; // race is finished - stop running

        // Take my turn
        if (UseRandomSteps)
        {
            if (Random.Shared.Next(100) == 0)   // 1 in 100 chance
                position[id]++;
        }
        else
        {
            position[id]++;                     // always step
        }

        // Tell main I'm done with my turn
        goMain[id].Set();
    }
}

// ---------------- Drawing ----------------
void DrawScreen()
{
    Console.Clear();
    Console.WriteLine($"Turn {turn}");
    Console.WriteLine();

    for (int i = 0; i < NumHorses; i++)
    {
        // e.g.  Horse 1: |-------H                    | 8
        string track = new string('-', position[i]) + "H";
        Console.WriteLine($"Horse {i + 1:D2}: |{track.PadRight(RaceLength + 1)}| {position[i]:D2}");
    }
    Console.WriteLine();

    if (winners.Count == 1)
    {
        Console.WriteLine($"Horse {winners[0] + 1:D2} WINS!");
    }
    else if (winners.Count > 1)
    {
        Console.Write("It's a TIE between horses:");
        foreach (int w in winners)
            Console.Write($" {w + 1:D2}");
        Console.WriteLine();
    }
}
