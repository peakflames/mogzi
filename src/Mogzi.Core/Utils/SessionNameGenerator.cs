namespace Mogzi.Core.Utils;

/// <summary>
/// Utility class for generating creative session names using random combinations of colors, creatures, fruits, and ice cream flavors.
/// </summary>
public static class SessionNameGenerator
{
    private static readonly Random Random = new();

    private static readonly string[] Colors =
    [
        "amber", "aqua", "azure", "beige", "black", "blue", "bronze", "brown", "burgundy", "charcoal",
        "coral", "cream", "crimson", "cyan", "ebony", "emerald", "fuchsia", "gold", "gray", "green",
        "indigo", "ivory", "jade", "khaki", "lavender", "magenta", "maroon", "navy", "ochre", "olive",
        "orange", "pearl", "pink", "purple", "red", "rose", "ruby", "rust", "sage", "salmon",
        "sand", "sapphire", "scarlet", "sepia", "silver", "slate", "snow", "tan", "teal", "topaz",
        "turquoise", "umber", "violet", "white", "wine", "yellow", "zinc", "cerulean", "cobalt",
        "copper", "garnet", "mahogany", "onyx", "pewter", "platinum", "steel", "titanium", "vermillion",
        "chartreuse", "mauve", "periwinkle", "taupe"
    ];

    private static readonly string[] Fruits =
    [
        "apple", "apricot", "avocado", "banana", "blackberry", "blueberry", "cherry", "coconut", "cranberry",
        "fig", "grape", "grapefruit", "guava", "kiwi", "lemon", "lime", "mango", "melon", "nectarine",
        "orange", "papaya", "passionfruit", "peach", "pear", "pineapple", "plum", "pomegranate", "raspberry",
        "strawberry", "tangerine", "watermelon", "dragonfruit", "lychee", "persimmon", "starfruit", "durian",
        "jackfruit", "rambutan", "mangosteen", "elderberry", "gooseberry", "currant", "boysenberry", "mulberry",
        "cantaloupe", "honeydew", "clementine", "mandarin", "kumquat", "plantain"
    ];

    private static readonly string[] IceCreamFlavors =
    [
        "vanilla", "chocolate", "strawberry", "mint", "cookies", "caramel", "butterscotch", "pistachio",
        "rocky", "neapolitan", "sherbet", "sorbet", "fudge", "toffee", "praline", "marshmallow",
        "bubblegum", "cotton", "rainbow", "birthday", "cookie", "brownie", "cheesecake", "tiramisu",
        "espresso", "mocha", "cappuccino", "hazelnut", "almond", "coconut", "peanut", "pecan",
        "walnut", "cashew", "macadamia", "honey", "maple", "cinnamon", "ginger", "lavender",
        "rose", "lemon", "orange", "berry", "swirl", "ripple", "chunk", "chip", "crunch", "twist"
    ];

    private static readonly string[] Creatures =
    [
        // Real creatures
        "aardvark", "albatross", "alligator", "alpaca", "ant", "anteater", "antelope", "ape", "armadillo", "badger",
        "barracuda", "bat", "bear", "beaver", "bee", "bison", "boar", "buffalo", "butterfly", "camel",
        "capybara", "caribou", "cassowary", "cat", "caterpillar", "cattle", "chamois", "cheetah", "chicken", "chimpanzee",
        "chinchilla", "chough", "clam", "cobra", "cockroach", "cod", "cormorant", "coyote", "crab", "crane",
        "crocodile", "crow", "curlew", "deer", "dinosaur", "dog", "dogfish", "dolphin", "donkey", "dotterel",
        "dove", "dragonfly", "duck", "dugong", "dunlin", "eagle", "echidna", "eel", "elephant",
        "elk", "emu", "falcon", "ferret", "finch", "fish", "flamingo", "fly", "fox", "frog",
        "gaur", "gazelle", "gerbil", "giraffe", "gnat", "gnu", "goat", "goldfinch", "goose", "gorilla",
        "goshawk", "grasshopper", "grouse", "guanaco", "gull", "hamster", "hare", "hawk", "hedgehog",
        "heron", "herring", "hippopotamus", "hornet", "horse", "hummingbird", "hyena", "ibex", "ibis", "iguana",
        "impala", "jackal", "jaguar", "jay", "jellyfish", "kangaroo", "kingfisher", "koala", "kookabura", "kouprey",
        "kudu", "lapwing", "lark", "lemur", "leopard", "liger", "lion", "llama", "lobster", "locust",
        "loris", "louse", "lyrebird", "lynx", "magpie", "mallard", "manatee", "mandrill", "mantis", "marten",
        "meerkat", "mink", "mole", "mongoose", "monkey", "moose", "mosquito", "mouse", "mule", "narwhal",
        "newt", "nightingale", "octopus", "okapi", "opossum", "oryx", "ostrich", "otter", "owl", "oyster",
        "panther", "parrot", "partridge", "peafowl", "pelican", "penguin", "pheasant", "pig", "pigeon",
        "pony", "porcupine", "porpoise", "quail", "quelea", "quetzal", "rabbit", "raccoon", "rail", "ram",
        "rat", "raven", "reindeer", "rhinoceros", "rook", "salamander", "salmon", "sandpiper", "sardine", "scorpion",
        "seahorse", "seal", "shark", "sheep", "shrew", "skunk", "snail", "snake", "sparrow", "spider",
        "spoonbill", "squid", "squirrel", "starling", "stingray", "stinkbug", "stork", "swallow", "swan", "tapir",
        "tarsier", "termite", "tiger", "toad", "trout", "turkey", "turtle", "viper", "vulture",
        "wallaby", "walrus", "wasp", "weasel", "whale", "wildcat", "wolf", "wolverine", "wombat", "woodcock",
        "woodpecker", "worm", "wren", "yak", "zebra",
        
        // Mythical creatures and D&D monsters
        "basilisk", "behemoth", "chimera", "cockatrice", "dragon", "drake", "gargoyle", "griffin", "hydra",
        "kraken", "leviathan", "manticore", "minotaur", "pegasus", "phoenix", "roc", "sphinx", "unicorn", "wyvern",
        
        // D&D races
        "dwarf", "elf", "halfling", "human", "dragonborn", "gnome", "tiefling", "orc", "goblin", "hobgoblin",
        "kobold", "lizardfolk", "tabaxi", "aarakocra", "genasi", "aasimar", "firbolg", "goliath", "kenku", "triton",
        
        // D&D monsters and creatures
        "aboleth", "ankheg", "beholder", "bulezau", "carrion", "displacer", "doppelganger", "ettin", "flumph", "gelatinous",
        "harpy", "illithid", "jabberwock", "kappa", "lich", "mindflayer", "naga", "owlbear", "pseudodragon", "quaggoth",
        "rakshasa", "shambling", "tarrasque", "umber", "vampire", "wraith", "xorn", "yuan", "zombie",
        
        // Additional D&D creatures
        "banshee", "centaur", "dryad", "elemental", "faerie", "giant", "hippogriff", "imp", "jackalwere", "kelpie",
        "lamia", "medusa", "nightmare", "ogre", "pixie", "quasit", "revenant", "satyr", "treant", "unicorn",
        "valkyrie", "wendigo", "xvart", "yeti", "zephyr"
    ];

    /// <summary>
    /// Generates a random session name in the format "item1_item2" using colors, creatures, fruits, and ice cream flavors.
    /// Ensures the two items come from different categories and are not duplicates.
    /// </summary>
    /// <returns>A randomly generated session name using lowercase items from different categories separated by an underscore.</returns>
    public static string GenerateName()
    {
        // Define the four categories
        var categories = new[]
        {
            ("Colors", Colors),
            ("Fruits", Fruits),
            ("IceCreamFlavors", IceCreamFlavors),
            ("Creatures", Creatures)
        };

        // Pick two different categories
        var firstCategoryIndex = Random.Next(categories.Length);
        int secondCategoryIndex;
        do
        {
            secondCategoryIndex = Random.Next(categories.Length);
        } while (secondCategoryIndex == firstCategoryIndex);

        var firstCategory = categories[firstCategoryIndex];
        var secondCategory = categories[secondCategoryIndex];

        // Pick random items from each category
        var firstItem = firstCategory.Item2[Random.Next(firstCategory.Item2.Length)];
        string secondItem;

        // Ensure no duplicates (in case different categories have overlapping items)
        do
        {
            secondItem = secondCategory.Item2[Random.Next(secondCategory.Item2.Length)];
        } while (secondItem == firstItem);

        return $"{firstItem}_{secondItem}";
    }
}
