using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hello
{
    internal class hello2
    {
        // Made the method static so it can be called like: hello2.interestpaid(...)
        public static void interestpaid(string userName, int retireYear, int currentYear, double new_balance)
        {
            int ytr = retireYear - currentYear;
            double storebal = new_balance;  // Declare OUTSIDE the loop
            for (int i = 0; i < ytr; i++)
            {
                new_balance = new_balance + (new_balance * 0.05); // Assuming a fixed interest rate of 5%

            }
            double interestRate = 0.05; // Assuming a fixed interest rate of 5%
            double totalInterest = new_balance - storebal;
            double totalAmount = new_balance;
            Console.WriteLine($"{userName}, you will earn {totalInterest} in interest by the time you retire.");
            Console.WriteLine($"{userName}, your total amount at retirement will be: {totalAmount}");
        }
    }
}
