namespace SnippetRunner.Applications
{
    public class MiniAdventureGame : ISnippet
    {
        public string Name => "MiniAdventureGame";
        public string Description => "Survive 3 Rooms";
        public int playerHP = 100;
        public int playerGold = 0;
        public string playerName = "";

        public Random random = new();
        public void Run(string[] args)
        {
            StartGame();
        }
       public void StartGame()
        {
            Console.WriteLine("Welcome to the Mini Adventure Game!");
            Console.Write("Enter your name, adventurer: ");
            playerName = Console.ReadLine()??"";

            Console.WriteLine($"\n Hello {playerName}! you have {playerHP} HP and {playerGold} gold.");
            Console.WriteLine("Your Quest Begins...\n");

            for (int room = 1; room <= 3; room++)
            {
                Console.WriteLine($"Room {room}");
                PlayRoom();
                if (playerHP <= 0)
                {
                    Console.WriteLine("You died in the Dungeon!");
                    EndGame();
                    return;
                }
                Console.WriteLine("----------------------\n");
            }
            Console.WriteLine("You've survived all the rooms!");
            EndGame();
        }

        public void PlayRoom()
        {
            Console.WriteLine("You have 3 choices:");
            Console.WriteLine("1. Fight a monster");
            Console.WriteLine("2. Search for treasure");
            Console.WriteLine("3. Try to run");

            Console.Write("Your choice (1-3): ");
            string choice = Console.ReadLine()??"";

            switch (choice)
            {
                case "1":
                    FightMonster();
                    break;
                case "2":
                    SearchTreasure();
                    break;
                case "3":
                    TryToRun();
                    break;
                default:
                    Console.WriteLine("😵 Invalid choice. You panic and lose 5 HP.");
                    playerHP -= 5;
                    break;
            }
            Console.WriteLine($"HP: {playerHP}, Gold: {playerGold}");
        }
        public void FightMonster()
        {
            int monsterDamage = random.Next(10, 26);
            Console.WriteLine("You fight a monster!");
            Console.WriteLine($"It hits you for {monsterDamage} damage.");
            playerHP -= monsterDamage;
        }

        public void SearchTreasure()
        {
            int goldFound = random.Next(10, 51);
            Console.WriteLine("You search the room and find treasure!");
            Console.WriteLine($"You collect {goldFound} gold");
            playerGold += goldFound;
        }

        public void TryToRun()
        {
            bool escaped = random.Next(0, 2) == 1;
            if (escaped)
            {
                Console.WriteLine("You successfully escaped!");
            }
            else
            {
                int penalty = random.Next(5, 16);
                Console.WriteLine("You Stumbled while running and hurt yourself!");
                Console.WriteLine($"You lose {penalty} HP.");
                playerHP -= penalty;
            }
        }

        public void EndGame()
        {
            Console.WriteLine("\n--- Game Over ---");
            Console.WriteLine($"Thanks for playing, {playerName}!");
            Console.WriteLine($"Final Stats: ❤️ HP = {playerHP}, 💰 Gold = {playerGold}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(); 
        }
    
    }
}