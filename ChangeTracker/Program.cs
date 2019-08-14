using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Oven;

namespace ChangeTracker
{
    public interface Player : IObserved
    {
        string Name { get; set; }
        IObservedCollection<string> Items { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            var player = ObservedEntity.Create<Player>();

            Console.WriteLine(player.HasChanges); // false

            player.Name = "asdfasdf";

            foreach (var c in player.UncommitedRevision.Changes)
                Console.WriteLine(c.After);

            Console.WriteLine(player.HasChanges); // true
            player.ConfirmChanges();

            Console.WriteLine(player.HasChanges); // false


            player.Items.Add("SADF");

            Console.WriteLine(player.Items.HasChanges);
        }
    }
}
