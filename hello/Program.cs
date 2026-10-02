using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hello
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // for comments
            /* multiple
             line comment */
            //Console.WriteLine("enter the number to be checked");
            //int rah = Convert.ToInt32(Console.ReadLine());
            //int rah;
            //while (!int.TryParse(Console.ReadLine(), out rah))
            //{
            //    Console.WriteLine("Invalid input. Enter a valid integer:");
            //}
            //string var1;
            //float b = 34.4F;
            //bool ItsGREAT = true;
            //char B = 'R';
            //string C = "Rahul loves priya &";
            //string a = "priya is wife";
            //string sadd = C + " " + a;
            //   var1 = Console.ReadLine();
            //  Console.WriteLine(var1);
            //  Console.WriteLine("hello c #");
            //  Console.Write("hello rahul #"+ rah);
            //Console.WriteLine("learning");
            //Console.WriteLine(ItsGREAT);
            //Console.WriteLine(C);
            ////Console.WriteLine(B);
            //Console.WriteLine(a);
            ////Console.ReadLine();
            //Console.WriteLine(sadd);
            //if (rah % 2 == 0 )
            //{
            //    Console.WriteLine("number is even");
            //}
            //else
            //{
            //    Console.WriteLine("number is odd");
            Console.WriteLine(" !!!! welcome to BUBU DUDU BANK !!!!");
            Console.WriteLine("Enter your name :");
            string name = Console.ReadLine();
            Console.WriteLine("Enter your Age :");
            int age = Convert.ToInt32(Console.ReadLine());
            validage(name, age);
            Console.WriteLine("Enter you old account balance :");
            double Old_balance = Convert.ToDouble(Console.ReadLine());
            Console.WriteLine("Enter the amount you want to deposit :");
            double deposit_amount = Convert.ToDouble(Console.ReadLine());
            double newBalance = totalbalance(Old_balance, deposit_amount, name);
            Console.WriteLine("enter in which year you awant to retire: ");
            int retire_year = Convert.ToInt32(Console.ReadLine());
            int current_year = DateTime.Now.Year;
            hello2.interestpaid(name, retire_year, current_year, newBalance);

            void validage(string userName, int userAge)
            {
                switch (userAge)
                {
                    case int n when n < 18:
                        Console.WriteLine($"Sorry {userName} you are not eligible to open an account.");
                        break;
                    default:
                        Console.WriteLine($"Congratulations {userName}! You are eligible to open an account.");
                        break;
                }
            }
            double totalbalance(double oldBalance, double depositAmount, string userName)
            {
                double updatedBalance = oldBalance + depositAmount;
                Console.WriteLine($"{userName}, your new account balance is: {updatedBalance}");
                return updatedBalance;
            }

        }
    }
}

