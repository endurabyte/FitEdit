using Ooui;
using System;
using Xamarin.Forms;

namespace Dauer.Ooui
{
    class Program
    {
        static void Main(string[] args)
        {
            Forms.Init();
            var page = new Page1();
            UI.Port = 8000;
            UI.Host = "localhost";

            UI.Publish("/", page.GetOouiElement());
            //UI.Present("/");

            //Console.ReadKey();
        }
    }
}
