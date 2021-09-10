using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2k21Extractor
{
    class Teams
    {
        public static List<SelectableTeam> TeamList = new List<SelectableTeam>()
        {
            new SelectableTeam("76ers", 0),
            new SelectableTeam("Bucks",1),
            new SelectableTeam("Bulls",2),
            new SelectableTeam("Cavaliers",3),
            new SelectableTeam("Celtics",4),
            new SelectableTeam("Clippers",5),
            new SelectableTeam("Grizzlies",6),
            new SelectableTeam("Hawks",7),
            new SelectableTeam("Heat",8),
            new SelectableTeam("Hornets",9),
            new SelectableTeam("Jazz",10),
            new SelectableTeam("Kings",11),
            new SelectableTeam("Knicks",12),
            new SelectableTeam("Lakers",13),
            new SelectableTeam("Magic",14),
            new SelectableTeam("Mavericks",15),
            new SelectableTeam("Nets",16),
            new SelectableTeam("Nuggets",17),
            new SelectableTeam("Pacers",18),
            new SelectableTeam("Pelicans",19),
            new SelectableTeam("Pistons",20),
            new SelectableTeam("Raptors",21),
            new SelectableTeam("Rockets",22),
            new SelectableTeam("Spurs",23),
            new SelectableTeam("Suns",24),
            new SelectableTeam("Thunder",25),
            new SelectableTeam("Timberwolves",26),
            new SelectableTeam("Trailblazers",27),
            new SelectableTeam("Warriors",28),
            new SelectableTeam("Wizards",29)
        };

        public static SelectableTeam GetTeam(string name)
        {
            return TeamList.FirstOrDefault(t => t.Name == name);
        }
    }

    public class SelectableTeam
    {
        public string Name { get; set; }
        public int TeamId { get; set; }
        public SelectableTeam(string name, int teamId)
        {
            Name = name;
            TeamId = teamId;
        }
    }
}
