using JSONOperationsLibrary;

//Serialization
var person = new { Name = "John", Age = 30, IsStudent = false };
string json = JSONOperations.Serialize(person);
Console.WriteLine("Serialization : " + json); // Output: {"Name":"John","Age":30,"IsStudent":false}

//Deserialization
json = "{\"Name\":\"John\",\"Age\":30,\"IsStudent\":false}";
var dict = JSONOperations.Deserialize(json) as Dictionary<string, object>;
Console.WriteLine("Deserialization : " + dict["Name"]); // Output: John
Person.test();

//Stream Operations
person = new { Name = "John", Age = 30, IsStudent = false };
using (var stream = new MemoryStream())
{
    JSONOperations.SerializeToStream(person, stream);
    stream.Position = 0;
    var deserializedPerson = JSONOperations.DeserializeFromStream(stream);
    Console.WriteLine("Stream Operations : " + deserializedPerson); // Output: Dictionary with person data
}


//Schema Validation
var schema = new Dictionary<string, Type>
{
    { "Name", typeof(string) },
    { "Age", typeof(int) },
    { "IsStudent", typeof(bool) }
};

 json = "{\"Name\":\"John\",\"Age\":30,\"IsStudent\":false}";
bool isValid = JSONOperations.ValidateSchema(json, schema);
Console.WriteLine("Schema Validation : " +isValid); // Output: True













public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public bool IsStudent { get; set; }
    public static void test()
    {
       
    string json = "{\"Name\":\"John\",\"Age\":30,\"IsStudent\":false}";
    Person person = JSONOperations.Deserialize<Person>(json);
    Console.WriteLine("Deserialization to Custom Type : " + person.Name); // Output: John

    }
}

