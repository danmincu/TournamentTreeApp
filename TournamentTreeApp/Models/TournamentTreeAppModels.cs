using Newtonsoft.Json;

namespace TournamentsTreeApp.Models
{


    public class Rootobject
    {
        public static Rootobject TestData()
        {
            string testDataJsonString = "{\"id\":\"93d7eef1 - 75b8 - 408a - aebc - 18b6eca2ace6\",\"data\":{\"bracket\":{\"dir\":\"lr\",\"init\":{\"results\":[[[[0,1],[0,1],[1,0],[1,0]],[[0,1],[1,0]],[[null,null]]],[[[0,1],[1,0]],[[1,0],[0,1]],[[null,null]],[[null,null]]],[[[null,null],[null,null]]]],\"teams\":[[\"Team 1\",\"Team 2\"],[\"Team 3\",\"Team 4\"],[\"Team 5\",\"Team 6\"],[\"Team 7\",\"Team 8\"]]},\"skipConsolationRound\":false,\"skipGrandFinalComeback\":false,\"skipSecondaryFinal\":false},\"name\":\"New tournament\"}}";
            return JsonConvert.DeserializeObject<Rootobject>(testDataJsonString);
        }

        public string id { get; set; }
        public Data data { get; set; }
    }

    public class Data
    {
        public Bracket bracket { get; set; }
        public string name { get; set; }
    }

    public class Bracket
    {
        public string dir { get; set; }
        public Init init { get; set; }
        public bool skipConsolationRound { get; set; }
        public bool skipGrandFinalComeback { get; set; }
        public bool skipSecondaryFinal { get; set; }
    }

    public class Init
    {
        public int?[][][][] results { get; set; }
        public string[][] teams { get; set; }
        public string[][] teamsIds { get; set; }
        public string divisionId { get; set; }
        public bool drawBracket { get; set; }
    }
}