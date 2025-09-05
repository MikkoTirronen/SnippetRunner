using System;

namespace SnippetRunner.Snippets.SecondWeek
{
    class PrintDecimalClass : ISnippet
    {
        public string Name => "PrintDecimal";
        public string Description => "Prints a decimal in percentage form";

        public void Run(string[] args)
        {
            if (args.Length > 0 && double.TryParse(args[0], out double cliInput))
            {
                PrintConversion(cliInput);
            }
            else
            {
                PrintConversion();
            }
        }

        public double GetInput()
        {
            Console.WriteLine("Enter a decimal to convert:");
            string? input = Console.ReadLine();

            if (double.TryParse(input, out double userInput))
            {
                return userInput;
            }
            else
            {
                Console.WriteLine("Invalid input! Defaulting to 0.");
                return 0;
            }
        }

        public double ConvertValue(double value)
        {
            return value * 100;
        }

        public void PrintConversion(double commandLineInput)
        {
            Console.WriteLine($"Percentage Conversion: {ConvertValue(commandLineInput)}%");
        }

        //
        public void PrintConversion()
        {
            double userInput = GetInput();
            Console.WriteLine($"Percentage Conversion: {ConvertValue(userInput)}%");
        }
    }
}
