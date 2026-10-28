// Layout data (see docs/DESIGN.md "World"). Row 0 is the NORTH edge of the map.
// Town, Forest and Ford are painted by a small generator script and pasted here; edit by hand freely.
// Legend:  . grass  , path  * flowers  ~ water  b bridge  ; sand  j reeds  v crops  k mushrooms  o cobbles
//          T tree-wall  t tree  h bush  r rock  f fence  l lantern  s sign  n notice board
//          X cliff/cave wall  M mine entrance  E mine exit  : stone/cave floor  = wood floor  _ rug
//          W wall  w window  D door (outdoors with no warp = locked)  U/R/G roofs (blue/red/gold)
//          C counter  a table  x crate/barrel  B bed  c chest  A altar  g dragon egg
//          S dragon statue  O well  m market stall  P ruined pillar  F campfire
//          Y fairy ring  q hidden spot (searched with a skill check, see ZoneDef.Secrets)
namespace DnD.Game
{
    public static class Maps
    {
        public static readonly string[] Town =
        {
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX~~XXXXXXXXX",
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX~~XXXXXXXXX",
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX~~XXXXXXXX",
            "TT....X.XXXXXXXXX.X..XXXXXXXXXXXXX.XX..X~~....XhXT",
            "TT.GGGGGGGGG.r.......XXXXMMXX.XXX........~~.....TT",
            "TT.GGGGGGGGG....h.....r..,,s......*..*...~~.....TT",
            "TT.GGGGGGGGG......t.....,,,.RRRRRRRRR....~~ttttt.T",
            "TT.GGGGGGGGG....t.......,,,.RRRRRRRRR....~~.....TT",
            "TttWwWWWWWwW........*...,,..RRRRRRRRR....~~t.t.t.T",
            "T*.WWWWDWWWW...........,,,..WwWWWWWwW....~~.....TT",
            "Tt.....,...UUUUUUU.....,,,..WWWWDWWWW...j~~t*t.tTT",
            "T***..*,.*.UUUUUUU.....l,,,.....,.......~~......TT",
            "TT*..*.,..*UUUUUUU......,,,.....,.*....j~~.t*t.tTT",
            "TT.*..,,.*.WwWWWwW.ooooooooooooo,......j~~......*T",
            "TT..,,,....WWWDWWWlnooooooooooool.....j~~..t.t.t.T",
            "T...,.......*.,...ooooooooooooooo.....j~~.......TT",
            "TTs.,..l......,...oooooooSooooooo....j~~......s.TT",
            "TT..,......,,,,,,,ooooooooooooooo..l..~~.......,,T",
            ",,,,,,,,,,,,,,,,,,ooooooooooooooo,,,,~~......,,,,,",
            ",,,,,,,,,,,,,.....ooooooooooooooo,,,,bb,,,,,,,,,,,",
            "TT............l...ooooooooooooooo..,,bb,,,,,,,,.TT",
            "T*r...............oooOoooooommmoo....~~~....l....T",
            "TT.......t.......,loooooooooooool....~~~.........T",
            "TT.........t....,,.ooooooooooooo....j~~~.*....*..T",
            "T.....RRRRR....,,...................j~~~...*....TT",
            "T.....RRRRR....,...........GGGGG.....j~~~.......TT",
            "TT....RRRRR...,ffff,,ffff..GGGGG.....j~~~........T",
            "TT.*.tWwWwW...,f,,,,,,,,f..GGGGG...;;j~~~~;;;;..TT",
            "TT....WWDWW*..,fvvvvvvvvf..WwWwW.;;~~~~~~~~~~~;;.T",
            "T*...**.,....,,f,,,,,,,,f..WWDWW;;~~~~~~~~~~~~~;;T",
            "T.......,,,,,,.fvvvvvvvvf....,..;~~~~~~~~~~~~~~~;T",
            "T..............f,,,,,,,,f,,,,,..;~~~~~~~~~~~~~~~;T",
            "T......*.......fvvvvvvvvf.......;;~~~~~~~~~~~~~;;T",
            "T..............ffffffffff........q;~~~~~~~~~~~;;.T",
            "TT.T....TT.TTTTT.TTT.TTT...TTT..TTT;;;;~~~;;;;..TT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        public static readonly string[] Forest =
        {
            "TTTTTTTTTTTTTTTTTTTTTTTTT~~TTTTTTTTTTTTTTTTTTT",
            "TTT.TTTTTTtTTT.TT..TTTTTT~~TTT.TTTTTT....TTTTT",
            "T.T.T..TT.T.TTT.TTT..TTTT~~TTT.TTTTTTh.T*TT.TT",
            "TT.....~~~...............P:::P:::P.........TTT",
            "T.T..~~~~~~~...ttt..*....:::::::q:rrh..h...TTT",
            "TTT.~~~~~~~~~.ttttt......::::s::::......tr.TTT",
            "TTT.~~~~~~~~~.ttttt.....~:r:::::::......*...TT",
            "TT...~~~~~~~..ttttt..h..~:::::::::.t......tTTT",
            "T.*....~~~.....ttt......~P:::::::Pr...t...tTTT",
            "T..........t.*.t....t..~~.....,.........r.tTtT",
            "T.T....t*ttt......ttt.~~...t..,......,,..ttTTT",
            "TT*..............ttt..~~......,,th..tt,...tT.T",
            "TT.......*.......ttt.~~..t*..tt,tt.ttt,t...TTT",
            "TT...............tt..~~....h.tt,tt.ttt,t...TTT",
            "TT*...............t.~~.......t,,tt.ttt,t..hTTT",
            "T.T............h....~~..t.h...,tt...tt,.h..TTT",
            "TTT......kkk...*...~~..ttt...,,,,,,,..,....sTT",
            "TTT.....k...k.....,bb,,,,,,,,,,,,,,,,,,tt..T.T",
            "T.T....k..Y..k.h.,,bb,,,,,,,,,,ttt,,,,,,,,,,,,",
            "TTT.*...k...k....,,b~.t.tttttttttt.t,,,,,,,,,,",
            "T........kkk..k..,.~~.t.ttttttttt..ttt.....TTT",
            "TT.t............,,,~~....ttttt.t..httt....*T.T",
            "TTT..h......h...,.,,~~....ttt.....*..ttt..t..T",
            "T...t..........,,tt,~~.......h.......ttt.....T",
            "TTT.......*...,,.tt,q~~..t......t..ttttt.....T",
            "TT...........,,..tt,.~~...........ttttt.....TT",
            "TTT.*..xx...,,.x.ttt..~~.......t.ttttttt.*.TTT",
            "TTT.........,....tttt.~~....tttt.ttttttt.....T",
            "T.T........F.....tt....~~....ttt.tttttttt...TT",
            "TT...............tt....~~....ttttttttttt*..T.T",
            "TTTt...c.....h.x.tt.....~~.......ttttttt...TTT",
            "TTTT.T...k.t..*..TT.*.TT~~T....TTTTTTTTT..TTTT",
            "T.T.TT.TTTT.TTTTTTT..TTTT~~.TTTTTT.T..T..TTT.T",
            "TTTTTTTTTTTTTTTTTTTTTTTTT~~TTTTTTTTTTTTTTTTTTT",
        };

        public static readonly string[] Ford =
        {
            "TTTTTTTTTTTTTTTTTTTTTTTTTTj~~TTTTTTTTTTTTTTTTT",
            "TTTTTTTT.T..TTT.TT.TT.TT..T~~j..T.TT..TTTTTTTT",
            "TT...........t.c...........~~jt..............T",
            "T...........t.x...xtt...r..~~..........t....TT",
            "TTt.P::::.......F..........~~.............r.TT",
            "TT..::q::....x............;;;;..............TT",
            "T...:::::...........,,,,,,,;;,,,..r......tttTT",
            "TT..::::P...........,.s....~~..,............TT",
            "TT..................,.....j~~j.,...........t.T",
            "T..............t....,.......~~.,r..........t.T",
            "T..t..r.....tt....t.,.......~~~,.............T",
            "T...t......t..t.....,t......~~~,............TT",
            "T.r.................,......j~~~,.............T",
            "T..s................,....s.~~~j,..........s..T",
            ",,,,,,,,,,,,,,,,,,,,,,,,,,,b~~,,,,,,,,,,,,,,TT",
            ",,,,,,,,,,,,,,,,,,,,,,,,,,,b~~,,,,,,,,,,,,,,TT",
            "TT.........................~~~.......,......TT",
            "TT.....t...................~~.....RRRRRRR...TT",
            "TT.....*......*.....*.....tt~~t...RRRRRRR...TT",
            "TT.........rh..h..*....t...j~~j..rWwWDWwW...TT",
            "TT.*...r......*..*..*......j~~j...r..,...xx.TT",
            "TT.....r.*..........t......~~....q..,,,......T",
            "T....t...*...*.....*.......~~.....t.........TT",
            "TT.t....r..r..**..*........~~r......tfff,fffTT",
            "TT....h...*................j~~q......fvvvvvfTT",
            "T..t.*.*...*...*.h....r.....~~.......fvvvvvf.T",
            "TT....h*...h.*.....*........~~.....t.fvvvvvfTT",
            "TT.........................j~~.......fffffffTT",
            "TTT.TT.T.TTTT..TTTT.T...TT.j~~T.T....T.TT.T.TT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTT~~TTTTTTTTTTTTTTTT",
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
