
```cs
interface Item : IObserved {
    int Quantity;
    string Name;
}
interface Player : IObserved {
    string Name;
    ICollection<Item> Items;
}
```

__Property tracking__
```cs
var player = ObservedEntity.Create<Player>();
player.Name = "John Doe";
player.HasChanges; // TRUE

player.ConfirmChanges(); 
player.HasChanges; // FALSE
```

__Collection tracking__
```cs
var item = ObservedEntity.Create<Item>();
item.Name = "Sword";
item.Quantity = 10;
player.Items.Add(item);
player.HasChanges; // TRUE
```

__Deep tracking__
```cs
player.ConfirmChanges();
player.HasChanges; // FALSE

item.Quantity = 15;
player.HasChanges; // TRUE
```

__Change revisions__
```cs
var rev = player.UncommitedRevision;
foreach (var c in rev.Changes) 
    Console.WriteLine($"{c.Key}, {c.Prev} => {c.After}");

var revisions = player.Revisions;
Console.WriteLine(revisions.Revision); // Revision No.
```

Performance Consideration
----
`IObserved` objects do more stuffs for tracking changes. It's definetly slower than non-tracking objects. Please be aware of the performance and do not use this for heavily changing objects. 