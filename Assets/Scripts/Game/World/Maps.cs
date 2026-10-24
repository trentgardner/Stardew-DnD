// Generated layout data (see docs/DESIGN.md "World"). Row 0 is the NORTH edge of the map.
// Legend:  . grass  , path  * flowers  ~ water  T tree-wall  t tree  h bush  r rock  f fence  l lantern
//          s sign  n notice board  X cave wall  M mine entrance  E mine exit  : stone/cave floor
//          = wood floor  _ rug  W wall  w window  D door  U/R/G roofs (shop/red/temple)
//          C counter  a table  x crate/barrel  B bed  c chest  A altar  g dragon egg
namespace DnD.Game
{
    public static class Maps
    {
        public static readonly string[] Town =
        {
            "TTTTTTTTTTTTTTTXXXXXXXXXXTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTXXXXXXXXXXTTTTTTTTTTTTTTT",
            "T..............XXXXMMXXXX..............T",
            "T..t...............,,s.................T",
            "T...*..............,,..*............*..T",
            "T............*.....,,l.................T",
            "T....UUUUUUU.......,,.....RRRRRRRRR....T",
            "T....UUUUUUU.......,,.....RRRRRRRRR....T",
            "T....UUUUUUUh...t..,,.....RRRRRRRRR....T",
            "T....WwWWWwW.......,,.t..hWwWWWWWwW..t.T",
            "T....WWWDWWW.......,,.....WWWWDWWWW....T",
            "T...h...,l....*....,,.........,l.......T",
            "T.......,..........,,....*....,.....h..T",
            "T.......,..........,,.n.......,........T",
            "T..,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,..T",
            "T..,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,..T",
            "T..................,,l.............*...T",
            "T...............t..,,..................T",
            "T....GGGGGGG.......,,......RRRRRR......T",
            "T....GGGGGGG.......,,......RRRRRR......T",
            "T.t..GGGGGGG*......,,......RRRRRR......T",
            "T....WwWWWwW.......,,...*..WwWWwW....t.T",
            "T....WWWDWWW...*...,,......WWDWWW......T",
            "T.......,l.........,,........,l........T",
            "T..,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,..T",
            "T..................,,...........~~~~~..T",
            "T............t...r.,,..t.......~~~~~~~.T",
            "T..*......*........,,....*.....~~~~~~~.T",
            "T..ffffffffff......,,...........~~~~~..T",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        public static readonly string[] Shop =
        {
            "WWWWWWWWWWWW",
            "W=x======x=W",
            "W==========W",
            "W=CCCCCCCC=W",
            "W==========W",
            "Wx=========W",
            "W=========xW",
            "W==========W",
            "WWWWWWDWWWWW",
        };

        public static readonly string[] Tavern =
        {
            "WWWWWWWWWWWWWWWW",
            "W========_____xW",
            "W=CCCCCC=======W",
            "W==============W",
            "W==========a===W",
            "W==============W",
            "W===a==========W",
            "W==========a===W",
            "Wx=====a=======W",
            "W==============W",
            "WWWWWWWWDWWWWWWW",
        };

        public static readonly string[] Temple =
        {
            "WWWWWWWWWWWW",
            "W::::AA::::W",
            "W::::__::::W",
            "W::::__::::W",
            "W:l::__::l:W",
            "W::::__::::W",
            "W::::__::::W",
            "W::::__::::W",
            "W::::__::::W",
            "WWWWWDDWWWWW",
        };

        public static readonly string[] Home =
        {
            "WWWWWWWWWW",
            "W========W",
            "W=B====c=W",
            "W========W",
            "W========W",
            "W======a=W",
            "W========W",
            "WWWWWDWWWW",
        };

        public static readonly string[] Mine =
        {
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            "XXX::::XXXXXX::::::g:::::::XXXXXXXXXXXXX",
            "XXX:c::XXXXXX:r::::::::::r:XXXXXXXXXXXXX",
            "XXX::::XXXXXX::::::::::::::XXXXXXXXXXXXX",
            "XXX::::XXXXXX::::::::::::::XXXXXXXXXXXXX",
            "XXX::::XXXXXX::::::::::::::XXXXXXXXXXXXX",
            "XXX::::XXXXXX::::::::::::::XXXXXXXXXXXXX",
            "XX::::::::XXX::::::::::::::XXXXXXXXXXXXX",
            "XX::::::r:XXX::::::::::::::XXXXXXXXXXXXX",
            "XX::::::::XXXXXXXXX::XXXXXXXXX:::::::::X",
            "XX::::::::XXXXXXXXX::XXXXXXXXX:::::::c:X",
            "XX::::::::XXXXXXXXX::XXXXXXXXX:::::::::X",
            "XX::::::::XXXXXXXXX::XXXXXXXXX:::::::::X",
            "XX::::::::XXXXXXXXX::XXXXXXXXX:::::::::X",
            "XX::::::::XXXXXXXXX::XXXXXXXXX:::::::::X",
            "XX::::::::XX::::::::::::::::XX:::::::::X",
            "XX::::::::XX::::::::::::::r:XX:::::::::X",
            "XXXX:::::::::::::::::::::::::::::::::::X",
            "XXXX:::::::::::::::::::::::::::::::::::X",
            "XXXX::::::::::r:::::::::::::::::::XXXXXX",
            "XXXXXXXXXXXX::::::::::::::::XXXXXXXXXXXX",
            "XXXXXXXXXXXXXXXXXX::::XXXXXXXXXXXXXXXXXX",
            "XXXXXXXXXXXXXXXXXX::::XXXXXXXXXXXXXXXXXX",
            "XXXXXXXXXXXXXXXXXX::::XXXXXXXXXXXXXXXXXX",
            "XXXXXXXXXXXXXXXXXX::::XXXXXXXXXXXXXXXXXX",
            "XXXXXXXXXXXXXXXXXX::::XXXXXXXXXXXXXXXXXX",
            "XXXXXXXXXXXXXXXXXX::::XXXXXXXXXXXXXXXXXX",
            "XXXXXXXXXXXXXXXXXX:EE:XXXXXXXXXXXXXXXXXX",
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
        };

    }
}
