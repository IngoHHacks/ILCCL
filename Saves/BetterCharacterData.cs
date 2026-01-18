using Newtonsoft.Json;
using System.Reflection;
using ILCCL.Content;
using JetBrains.Annotations;

// ReSharper disable InconsistentNaming
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace ILCCL.Saves;

internal class BetterCharacterData
{
    public int? absent;

    public int? activeCostume;

    public int? age;

    public int? agreement;

    public string alias;

    public float? angle;

    public int? anim;

    public float? armMass;

    public float? bodyMass;

    public int? cell;

    public int? chained;

    public int? clause;

    public int? contract;

    public BetterCostumeData[] costumeC = [];

    public int? costumeChange;

    public int? crime;

    public int? cuffed;

    public int? dead;

    public float? demeanor;

    public int?[] experience = [];

    public int? fed;

    public int? gender;

    public int? grudge;

    public float? headSize;

    public float? health;
    
    public float? healthLimit;

    public int? heel;

    public float? height;

    public int? home;

    public int? id;

    public int? injury;

    public int? injuryTime;

    public int?[] knowledge = [];

    public float? legMass;

    public int? light;

    public int? location;

    public int?[] moveAttack = [];

    public int?[] moveBack = [];

    public int?[] moveCrush = [];

    public int?[] moveFront = [];

    public int?[] moveGround = [];

    public Moveset[] moveset = [];

    public float? muscleMass;

    public string musicC;

    public float? musicSpeed;

    public string name;

    public int? negotiated;

    public int? news;

    public float?[] newStat = [];

    public string offspringC;

    public int? oldFed;

    public float?[] oldStat = [];
    
    public int? platform;

    public int? player;

    public int? possessive;

    public int? powerAttack;

    public int? powerCraft;

    public int? powerFlight;

    public int? powerGrapple;

    public int? powerInnate;

    public int? powerManipulate;
    
    public int? pregnant;

    public int?[] projectileLimb = [];

    public int? promo;

    public int? promoVariable;

    public int? prop;

    public int? pyro;

    public string[] relationC = [];

    public int? role;

    public int? salary;

    public int?[] scar = [];

    public int? seat;

    public float? spirit;

    public int? stance;

    public float?[] stat = [];

    public float?[] superStat = [];

    public int?[] taunt = [];

    public int? tauntHandshake;

    public int? tauntWave;

    public int? team;

    public string teamName;

    public int? toilet;

    public float? visibility;

    public float? voice;

    public int? worked;

    public int? worth;

    public int? warrant;

    public int? warrantVariable;

    public int? warrantVictim;

    public int? warrantWitness;

    public int? weakness;

    public float? x;

    public float? y;

    public float? z;

    public string GAME = "IL";
    
    public string VERSION = "2";

    public static BetterCharacterData FromRegularCharacter(Character character, Character[] allCharacters, bool ignoreRelations = false)
    {
        BetterCharacterData bcd =
            JsonConvert.DeserializeObject<BetterCharacterData>(JsonConvert.SerializeObject(character))!;
        bcd.costumeC = new BetterCostumeData[character.costume.Length];
        for (int i = 0; i < character.costume.Length; i++)
        {
            bcd.costumeC[i] = BetterCostumeData.FromRegularCostumeData(character.costume[i]);
        }

        bcd.relationC = new string[character.relation.Length];
        if (!ignoreRelations) {
            for (int i = 0; i < character.relation.Length; i++)
            {
                if (i == 0)
                {
                    bcd.relationC[i] = "0";
                    continue;
                }
                if (character.relation[i] >= allCharacters.Length)
                {
                    bcd.relationC[i] = "0";
                    continue;
                }

                bcd.relationC[i] = character.relation[i] == 0
                    ? "0"
                    : allCharacters[i].name + "=" + character.relation[i];
            }
        } else {
            for (int i = 0; i < character.relation.Length; i++)
            {
                bcd.relationC[i] = "0";
            }
        }

        bcd.grudge = 0;

        return bcd;
    }

    public Character ToRegularCharacter(Character[] allCharacters)
    {
        if (this.VERSION != "2")
        {
            return ToRegularCharacterV1(allCharacters);
        }
        Character character = JsonConvert.DeserializeObject<Character>(JsonConvert.SerializeObject(this),
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })!;
        character.costume = new Costume[this.costumeC.Length];
        for (int i = 0; i < this.costumeC.Length; i++)
        {
            if (this.costumeC[i] == null)
            {
                character.costume[i] = null;
                continue;
            }
            this.costumeC[i].charID = character.id;

            character.costume[i] = this.costumeC[i].ToRegularCostume();
        }

        character.relation = new int[Characters.no_chars + 2];
        if (this.GAME != "WE")
        {
            for (int i = 0; i < this.relationC.Length; i++)
            {
                if (i == 0)
                {
                    continue;
                }
                if (this.relationC[i] == "0")
                {
                    continue;
                }
                string[] split = this.relationC[i].Split('=');
                string name = split[0];
                try
                {
                    var id = allCharacters.Single(c => c != null && c.name != null && c.name == name).id;
                    character.relation[id] = int.Parse(split[1]);
                }
                catch (Exception)
                {
                    if (i >= character.relation.Length)
                    {
                        LogWarning("Failed to find character with name " + name + ", skipping because id is out of bounds.");
                        continue;
                    }
                    character.relation[i] = int.Parse(split[1]);
                    LogWarning("Failed to find character with name " + name + ", using id instead.");
                
                }
            }
        }
        if (this.GAME != "IL")
        {
            CopySplitMovesets(character);
        }
        EnsureArraySizes(character);
        character.grudge = 0;
        character.team = 0;

        return character;
    }
    
    public Character ToRegularCharacterV1(Character[] allCharacters)
    {
        Character character = JsonConvert.DeserializeObject<Character>(JsonConvert.SerializeObject(this),
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })!;
        character.costume = new Costume[this.costumeC.Length];
        for (int i = 0; i < this.costumeC.Length; i++)
        {
            if (this.costumeC[i] == null)
            {
                character.costume[i] = null;
                continue;
            }
            this.costumeC[i].charID = character.id;

            character.costume[i] = this.costumeC[i].ToRegularCostume();
        }

        character.relation = new int[Characters.no_chars + 2];
        if (this.VERSION.EndsWith("-HT"))
        {
            for (int i = 0; i < this.relationC.Length; i++)
            {
                if (i == 0)
                {
                    continue;
                }
                if (this.relationC[i] == "0")
                {
                    continue;
                }
                string[] split = this.relationC[i].Split('=');
                string name = split[0];
                try
                {
                    var id = allCharacters.Single(c => c != null && c.name != null && c.name == name).id;
                    character.relation[id] = int.Parse(split[1]);
                }
                catch (Exception)
                {
                    if (i >= character.relation.Length)
                    {
                        LogWarning("Failed to find character with name " + name + ", skipping because id is out of bounds.");
                        continue;
                    }
                    character.relation[i] = int.Parse(split[1]);
                    LogWarning("Failed to find character with name " + name + ", using id instead.");
                
                }
            }

            CopySplitMovesets(character);
        }
        character.grudge = 0;
        character.team = 0;

        return character;
    }
    
    private void CopySplitMovesets(Character character)
    {
        character.moveset = new Moveset[2];
        for (int i = 0; i < 2; i++)
        {
            var ms = new Moveset();
            character.moveset[i] = ms;
            ((MappedMoveset)ms).GenerateMoves();
            if (ms.moveFront.Length < this.moveFront.Length)
            {
                Array.Resize(ref ms.moveFront, this.moveFront.Length);
            }
            Array.Copy(this.moveFront, ms.moveFront, this.moveFront.Length);
            if (ms.moveBack.Length < this.moveBack.Length)
            {
                Array.Resize(ref ms.moveBack, this.moveBack.Length);
            }
            Array.Copy(this.moveBack, ms.moveBack, this.moveBack.Length);
            if (ms.moveAttack.Length < this.moveAttack.Length)
            {
                Array.Resize(ref ms.moveAttack, this.moveAttack.Length);
            }
            Array.Copy(this.moveAttack, ms.moveAttack, this.moveAttack.Length);
            if (ms.moveCrush.Length < this.moveCrush.Length)
            {
                Array.Resize(ref ms.moveCrush, this.moveCrush.Length);
            }
            Array.Copy(this.moveCrush, ms.moveCrush, this.moveCrush.Length);
            if (ms.moveGround.Length < this.moveGround.Length)
            {
                Array.Resize(ref ms.moveGround, this.moveGround.Length);
            }
            Array.Copy(this.moveGround, ms.moveGround, this.moveGround.Length);
            if (ms.taunt.Length < this.taunt.Length)
            {
                Array.Resize(ref ms.taunt, this.taunt.Length);
            }
            Array.Copy(this.taunt, ms.taunt, this.taunt.Length);
            ms.stance = this.stance ?? ms.stance;
            ms.tauntWave = this.tauntWave ?? ms.tauntWave;
            ms.tauntHandshake = this.tauntHandshake ?? ms.tauntHandshake;
        }
    }
    
    private static void EnsureArraySizes(Character character)
    {
        foreach (FieldInfo field in typeof(Character).GetFields())
        {
            if (field.FieldType.IsArray)
            {
                Array array = (Array)field.GetValue(character);
                Array template = (Array)field.GetValue(Characters.c[1]);
                if (array == null)
                {
                    field.SetValue(character, template);
                }
                else if (array.Length < template.Length)
                {
                    Array newArray = Array.CreateInstance(template.GetType().GetElementType()!, template.Length);
                    Array.Copy(template, newArray, template.Length);
                    Array.Copy(array, newArray, array.Length);
                    field.SetValue(character, newArray);
                }
            }
        }
    }

    public void MergeIntoCharacter(Character character)
    {
        foreach (FieldInfo field in typeof(BetterCharacterData).GetFields())
        {
            if (field.FieldType.IsArray)
            {
                Array array = (Array)field.GetValue(this);
                if (array == null)
                {
                    continue;
                }

                bool allNull = true;
                bool allNonNull = true;
                foreach (object element in array)
                {
                    if (element != null)
                    {
                        allNull = false;
                    }
                    else
                    {
                        allNonNull = false;
                    }
                }

                if (allNull)
                {
                    field.SetValue(this, null);
                }
                else if (!allNonNull)
                {
                    throw new Exception("It is not possible to merge arrays with both null and non-null elements.");
                }
            }
        }

        // Ignore nulls and nulls in arrays
        JsonConvert.PopulateObject(JsonConvert.SerializeObject(this), character,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        character.grudge = 0;
        character.team = 0;
    }
}