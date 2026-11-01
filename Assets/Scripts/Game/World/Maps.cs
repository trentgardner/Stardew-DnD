// Layout data (see docs/DESIGN.md "World"). Row 0 is the NORTH edge of the map.
// Town, Forest and Ford are painted by a small generator script and pasted here; edit by hand freely.
// Legend:  . grass  , path  * flowers  ~ water  b bridge  ; sand  j reeds  v crops  k mushrooms  o cobbles
//          T tree-wall  t tree  h bush  r rock  f fence  l lantern  s sign  n notice board
//          X cliff/cave wall  M mine entrance  E mine exit  : stone/cave floor  = wood floor  _ rug
//          W wall  w window  D door (outdoors with no warp = locked)  U/R/G roofs (blue/red/gold)
//          C counter  a table  x crate/barrel  B bed  c chest  A altar  g dragon egg
//          S dragon statue  O well  m market stall  P ruined pillar  F campfire
//          Y fairy ring  q hidden spot (searched with a skill check, see ZoneDef.Secrets)
//          K anvil/forge  N bench  p planter  L washing line
//          d scorched earth  e glowing crystals  H kobold hut  i brazier  V stairs down  u tent
//          puzzle pieces (see ZoneDef.Puzzle): Q plate  I lever  J push block  Z gate  / mirror  @ light source  % light crystal
//          Academy (Act II): z bookshelf  # hedge  + ward pillar  ^ floating stone  & the Grand Orrery  $ the Starwell
//                            ! telescope  - desk  < rune floor (walkable)
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
            "TT.*..,,.*.WwWWWwW.pooooooooooop,......j~~......*T",
            "TT..,,,....WWWDWWWlnooooooooooool.....j~~..t.t.t.T",
            "T...,.......*.,...ooooooooooooooo.....j~~.......TT",
            "TTs.,..l......,...ommmoooSooooooo....j~~......s.TT",
            "TT..,......,,,,,,,ooooooooooooooo..l..~~.......,,T",
            ",,,,,,,,,,,,,,,,,,ooooooooooooooo,,,,~~......,,,,,",
            ",,,,,,,,,,,,,.....ooooooooooooooo,,,,bb,,,,,,,,,,,",
            "TT............l...ooooooooooooooo..,,bb,,,,,,,,.TT",
            "T*r...............oooOoooooommmoo....~~~....l....T",
            "TT.......t.......,lpooooooooooopl....~~~.........T",
            "TT.........t....,,.ooooNooooNooo....j~~~.*....*..T",
            "T.....RRRRR....,,...................j~~~...*....TT",
            "T.....RRRRR....,...........GGGGG.....j~~~.......TT",
            "TT....RRRRR...,ffff,,ffff..GGGGG.....j~~~........T",
            "TT.*.tWwWwW...,f,,,,,,,,f..GGGGG...;;j~~~~;;;;..TT",
            "TT....WWDWW*..,fvvvvvvvvf..WwWwW.;;~~~~~~~~~~~;;.T",
            "T*...**.,....,,f,,,,,,,,f..WWDWW;;~~~~~~~~~~~~~;;T",
            "T.......,,,,,,.fvvvvvvvvf....,..;~~~~~~~~~~~~~~~;T",
            "T.........,....f,,,,,,,,f,,,,,..;~~~~~~~~~~~~~~~;T",
            "T......*..,....fvvvvvvvvf.......;;~~~~~~~~~~~~~;;T",
            "T.........,....ffffffffff........q;~~~~~~~~~~~;;.T",
            "TT.T....TT,TTTTT.TTT.TTT...TTT..TTT;;;;~~~;;;;..TT",
            "TTTTTTTTTT,TTTTTTTTTTTTTTTTTTTTTTTTTTTT~~~TTTTTTTT",
            "T..RRRRRRR,............UUUUUUUU........~~~......TT",
            "TT.RRRRRRR,..xx.t....h.UUUUUUUU.RRRRR.j~~~.GGGGG.T",
            "TT.RRRRRRR,.K......t...UUUUUUUU.RRRRR..~~~jGGGGGTT",
            "T..WwWWWwW,,,,x..h.....WwWWWWwW.RRRRR..~~~.GGGGG.T",
            "TT.WWWDWWW,.........t..WWWWDWWW.WwWwW.j~~~.WwWwWTT",
            "TT....,,,,,l...t......l....,.s..WWDWW..~~~.WWDWW.T",
            "T.t.t.,...,................,...h..,....~~~...,..TT",
            "TT.h......,,,,,,,,,,,,,,,,,,,,,,,,,,,,,bbb,,,,,,.T",
            "TT.*..*...,,,,,,,,,,,,,,,,,,,,,,,,,,,,,bbb,,,,,,TT",
            "T.UUUUU.*.,.s....*....*..........*.*.*.~~~j....h.T",
            "TTUUUUU...,....N....N....*...t..ffffffj~~~ffffffTT",
            "TTUUUUU.L.,...*........................~~~.*.*...T",
            "T.WwWwW...,..p....t....p..*....t...h...~~~j.x.x.TT",
            "TTWWDWW...,.....*....*......*....t....j~~~.......T",
            "TT..,,,,,,,..t............h.........t..~~~..t.t.TT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        public static readonly string[] Forest =
        {
            "TTTTTTTTTTTTTTTTTTTTTTTTT~~TTT,,TTTTTTTTTTTTTT",
            "TTT.TTTTTTtTTT.TT..TTTTTT~~TTT,,TTTTT....TTTTT",
            "T.T.T..TT.T.TTT.TTT..TTTT~~TTT,,TTTTTh.T*TT.TT",
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
            "TTTTTTTTTTTTTTTTTTTTTTTTTTj~~TTTTTTT,,TTTTTTTT",
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
            ",,,,,,,,,,,,,,,,,,,,,,,,,,,b~~,,,,,,,,,,,,,,,,",
            ",,,,,,,,,,,,,,,,,,,,,,,,,,,b~~,,,,,,,,,,,,,,,,",
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

        public static readonly string[] Forge =
        {
            "WWWWWWWWWWWW",
            "W=x==K===x=W",
            "W==========W",
            "W=CCCCCCCC=W",
            "W==========W",
            "Wx=========W",
            "W========x=W",
            "W==========W",
            "WWWWWWDWWWWW",
        };

        public static readonly string[] WatchHouse =
        {
            "WWWWWWWWWWWW",
            "W=a=====fffW",
            "W=a=====f==W",
            "W=======f==W",
            "W==========W",
            "Wx======a==W",
            "W==========W",
            "WWWWWDWWWWWW",
        };

        public static readonly string[] CottageLark =
        {
            "WWWWWWWWWW",
            "W=B====x=W",
            "W========W",
            "W========W",
            "W====a===W",
            "W========W",
            "WWWWDWWWWW",
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
            "XXX::::XXXXXXy:::::V::::::yXXXXXXXXXXXXX",
            "XXX:c::XXXXXX:r::::::::::r:XXXXXXXXXXXXX",
            "XXX::::XXXXXX::::::::::::::XXXXXXXXXXXXX",
            "XXX::::XXXXXX::::::::::::::XXXXXXXXXXXXX",
            "XXX::::XXXXXX::::::::::::::XXXXXXXXXXXXX",
            "XXX::::XXXXXX::::::::::::::XXXXXXXXXXXXX",
            "XXy:::::::XXX::::::::::::::XXXXXXXXXXXXX",
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

        /// <summary>Phase 5: where the "star" came down, in the hills north of Whisperwood. South edge = the forest's north trail.</summary>
        public static readonly string[] Crater =
        {
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            "XXXXXXXXXXXXXX.XX..XX.X.XXXX.XX.XXXXX.XXXXXX",
            "XXXqXXXXX.X..X.X.............X........qXXXXX",
            "XXX,,,XXX............................,,XXXXX",
            "XXX,,,,XX...........r...............,,.....X",
            "XXXX,,,XX...............r..........,,.....XX",
            "XXXXXX,,X.........................,.......XX",
            "XXXXXXX,,............ddd........,,........XX",
            "XX......,...e...dddddddddddd...,.........XXX",
            "XX.............dddddddddddeddd...t.......t.X",
            "XX............ddeeddddddddddddd...........XX",
            "X............dddddddddddddddqddd...........X",
            "XXt.....r....dddddddddddddddddddd...r....X.X",
            "XXX......r..dddddddddddddddddeedd.........XX",
            "XXX.........ddddddddddqdddddddddd........XXX",
            "XX...r......ddddddddddddddddddddd........X.X",
            "X.X.........dddqdddddddddddddddd.....r....XX",
            "XXX.......e..ddddddddddddddddddd...........X",
            "XX............dddddddddddddeddd..........XXX",
            "XX.............dddeedddddddddd............XX",
            "XX...............ddddddddedd......e.......XX",
            "XXX......s......,,,,,,,,...................X",
            "X.....u..,,.,,,,.....,,...............r...XX",
            "XX...x..F.,,..r......,,....................X",
            "XX...................,,.......r............X",
            "X...........t........,,.................t..X",
            "XX.t...........t.....,,...t....t..........XX",
            "XX...............t...,,....Xt.......XXX..X.X",
            "X..XXXX..X.X..XXXXXXX,,XXX.X.XXXXXXX.XX.XXXX",
            "XXXXXXXXXXXXXXXXXXXXX,,XXXXXXXXXXXXXXXXXXXXX",
        };

        /// <summary>Phase 5: the kobold village below the old mine. Gate at the bottom, the village cavern and its stream, Krag's corridor top right.</summary>
        public static readonly string[] Warrens =
        {
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            "XXXXXXXXXXXXXXXXXXX:XXXXXXXXXXXXXXX::V:XXXXX",
            "XXXXXXXXXXXXXXXX::::iX:::X:XXXXXXXX::::XXXXX",
            "XXXXXXXXXXX:::::::::::::::::::::XXX::::XXXXX",
            "XXXXXXXc:X::H::::HH::::::H:::::::qX::::XXXXX",
            "XXXXXXX:H:::::::::::::::::::::::::X::::XXXXX",
            "XXXXXX:::::::::i:::::i:::::::H:::::::::XXXXX",
            "XXX:Xr:::::::::::::::::H:::::::::::::::XXXXX",
            "XXXX:::::::::H:::::::::::::::::::::::XXXXXXX",
            "XXXX::H:::::::::::::::::::::i::::::::XXXXXXX",
            "XXXX::::::i:::::::::::::::::::mm::::XXXXXXXX",
            "XXX:x::::::::::~~~bb~~~~::::::::::r:::XXXXXX",
            "XXXX:::::::~~~~~~~bb~~~~~~~~::::::::~~XXXXXX",
            "XXXX~~~~~~~~~~~:::::::::~~~~~~~~~~~X~XXXXXXX",
            "XXXXX~~~~~~k::::::::::::::::~~~~~~~X:XXXXXXX",
            "XXXXXXXkkkkk::::::::::::::H::::H:::XXXXXXXXX",
            "XXXXXXXkXkkk::i:::::::::i:::::::XXXXXXXXXXXX",
            "XXXXXXXXXXXX:::::::::::::::::::XXXXXXXXXXXXX",
            "XXXXXXXXXXXXXX:X::::::::X:XXXXXXXXXXXXXXXXXX",
            "XXXXXXXXXXXXXXXXqXXX::XXXXXXXXXXXXXXXXXXXXXX",
            "XXXXXXXXXXXXXX:::::::::::::XXXXXXXXXXXXXXXXX",
            "XXXXXXXXXXXXXy::::::::::::::XXXXXXXXXXXXXXXX",
            "XXXXXXXXX:::::::::::::::::::XXXXXXXXXXXXXXXX",
            "XXXXXX::::::::::::::::::::::XXXXXXXXXXXXXXXX",
            "XXXXXX:::::::::::::::::::::XXXXXXXXXXXXXXXXX",
            "XXXXXX::::XXXXXXX:::XX::XXXXXXXXXXXXXXXXXXXX",
            "XXXX::::::XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            "XXXX::::XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            "XXXX::::XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            "XXXX:EE:XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
        };

        /// <summary>Phase 5: the Shrine of the Great Egg, the deepest level of the warrens.</summary>
        public static readonly string[] EggShrine =
        {
            "XXXXXXXXXXXXXXXXXXXXXXXXXX",
            "XXXXXXXXXXXXXXXXXXXXXXXXXX",
            "XXXXX:::::::Ag::::::rXXXXX",
            "XXXXX::::i::::::i::::XXXXX",
            "XXX::::::::::::::::::::XXX",
            "XXX:q::::::::::::::::::XXX",
            "XXX:::i::::::::::::i:::XXX",
            "XXX::::::::::::::::::::XXX",
            "XXX:::::::::::::::::::rXXX",
            "XXXXXr:::::::::::::::XXXXX",
            "XXXXXXXXXXx:::::XXXXXXXXXX",
            "XXXXXXXXs::::::::sXXXXXXXX",
            "XXXXXXXX:x::::::x:XXXXXXXX",
            "XXXXXXXXXX::::::XXXXXXXXXX",
            "XXXXXXXXXXX::::XXXXXXXXXXX",
            "XXXXXXXXXXX::::XXXXXXXXXXX",
            "XXXXXXXXXXX:EE:XXXXXXXXXXX",
            "XXXXXXXXXXXXXXXXXXXXXXXXXX",
        };

        /// <summary>Phase 5: the Old Watchtower on the ridge north of Miller's Ford. Courtyard, then three puzzle rooms climbing to the lantern room.</summary>
        public static readonly string[] Watchtower =
        {
            "TTTTTWWWWWWWWWWWWWWWWWWWWWWTTTTT",
            "TTTT.WWW@WWWWWWWWWWWWWWWWWW.T..T",
            "T....W:::::::::%W::::q:::WW....T",
            "TT...W::::::::::Z:::::::cWW....T",
            "TT...W::/:::::/:W::::::::WW....T",
            "T....W::::::::::WWWWWWWWWWW...TT",
            "TT...WWWWWZWWWWWWWWWWWWWWWW...TT",
            "T....W::::::::::WWWWWWWWWWW...TT",
            "T....W:Q::::::Q:WWWWWWWWWWW...TT",
            "TT...W::::J:::::WWWWWWWWWWW...TT",
            "TT...Wr:::::::::WWWWWWWWWWW....T",
            "T....WWWWWZWWWWWWWWWWWWWWWW....T",
            "T....W::::::::::::I:I:I:x:W....T",
            "T....W::::::::::::::::::::W....T",
            "T....WP::::::::::::::::::rW...TT",
            "T....WWWWWWWWWW::WWWWWWWWWW....T",
            "T.............::::.............T",
            "T...t...h..t...::...h......t..TT",
            "T..............::......*.......T",
            "TT..Pr.rP.rrP..::..rPrrrPrrrP.TT",
            "T..............::..............T",
            "TT.......S.....::........t.....T",
            "TT.h........*..::.....t.......TT",
            "TT....t........::..*......h....T",
            "T.T.T.T..T.TT.T::.TT.T..TTT....T",
            "TTTTTTTTTTTTTTT,,TTTTTTTTTTTTTTT",
        };

        // Act II: the Lantern Road, Miller's Ford to the Starfall Academy's gate (north-east).
        public static readonly string[] LanternRoad =
        {
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT,,TTTTTT",
            "TTXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX..TT.TT,,T....T",
            "TTXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX.......,,....TT",
            "TTXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX......l,,....TT",
            "TTXXX..X...XXXXX......X.....X.X..XX..XXX..X.........,,....TT",
            "TTh..t....*..........t.....t....h........~~.........,,....TT",
            "TT......h...............X........t....h.j~~.......*t,,..h.TT",
            "TTr............t.t..:::::::::..*.......t.~~........l,,.....T",
            "T...................:i::q::i:..t.........~~...t.....,,....TT",
            "TTt.............t...:::::::::t...........~~.:u::u:l.,,....TT",
            "TT.......t..........:N:::::N:...........~~..:::q:::.,,....TT",
            "T.....*........t....:::::::::...........~~..:::a:::l,,.....T",
            "T.....................,,,,,...t......*..~~.hxx:::F:.,,.....T",
            "T.....l.....l.....l.....,.....l.....l...~~l.:::::::.,,..t.TT",
            ",,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,bb,,,,,,,,,,,,....TT",
            ",,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,bb,,,,,,,,,,,.t..TT",
            "T...............................,.......j~~................T",
            "T...ddddd.dd.dh.dddddddd.dd....,,,,,....j~~............t..TT",
            "T..rd.d.ddddddd.ddddddd.ddd....,,,,,....j~~....t*t........TT",
            "T...d.dd..dddddddd.d..dd..d..R.RRRR......~~r..tt.t.t.t.t...T",
            "T.*.dddddd..dddd*.d.dd..ddd..RRRR.R....j~~...ht*.h....t....T",
            "TT..dddddddddddd..rddd.ddd..tWWwDWW.....~~..ttthtt...t*...TT",
            "TT...ddtddddddq.dtd.d.ddd.d.....r....f..~~....ttttt...t....T",
            "T...ddddddddd.ddddddddddd.d........q.f..~~......ttt..t.....T",
            "TT...dd.ddd.d.ddddddddddd....xx......f..~~.....t.t.t..t...TT",
            "T..rdddddddd..dd*dddddddddd..........f..h~~....ht.....ttt..T",
            "T...dddddddd.dd.dd.dddd..dd.ffff.ffff....~~.....t.t...t....T",
            "T.......th....t.........................j~~......h.........T",
            "TTT.TT.T.TTTT..TTTT.T...TT.T..T.T....T.TT~~.T..T.TTTTT...T.T",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        // Act II: the Starfall Academy grounds. Gate (27-28, 43) south; library door (12,11), the Orrery Tower (44,13), the Observatory (48,25), the Glasshouse (8,26).
        public static readonly string[] Academy =
        {
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWT",
            "TW..+.....+.....+.....+.....+.....+.....+GGGGGGG....+.WT",
            "TW..UUUUUUUUUUUUUUUUU...h.......RRRRR....GGGGGGG......WT",
            "TW..UUUUUUUUUUUUUUUUU...........RRRRR....GGGGGGG......WT",
            "TW+.UUUUUUUUUUUUUUUUU.....t.....RRRRR....GGGGGGG.....+WT",
            "TW..UUUUUUUUUUUUUUUUU........t..RRRRR....GGGGGGG......WT",
            "TW..UUUUUUUUUUUUUUUUU........t..WWWWW....GGGGGGG......WT",
            "TW..UUUUUUUUUUUUUUUUU...*.......WWWWW....GGGGGGG......WT",
            "TW..WWwWWwWWWWWwWWwWW.......t.....q..s...GGGGGGG......WT",
            "TW..WWWWWWWWDWWWWWWWW....................GGGGGGG...t..WT",
            "TW+........l..l........................h.WWwWwWW.h...+WT",
            "TW..........,,,,,,,,,,,....S.............WWWDWWW......WT",
            "TW..........,,.........oooooo.oo..........+...+.......WT",
            "TW...*...............pooooooooo,,,,,,,,,,,,,,.........WT",
            "TW...................ooooooooooooo....................WT",
            "TW..t...t...h.......oooo^ooo^oooooo...................WT",
            "TW+................oooooooooooo^oooo........UUUUUUUUU+WT",
            "TW.GGGGGGGGGGG.....ooo^oo<<<<<oooooo......t.UUUUUUUUU.WT",
            "TW.GGGGGGGGGGG.....oooooo<ooo<oooooo........UUUUUUUUU.WT",
            "TW.GGGGGGGGGGG,,,,,oNoooo<o&o<oo^oNo,,,,,,,,UUUUUUUUU.WT",
            "TW.GGGGGGGGGGG,,,,,oooooo<ooo<oooooo,,,,,,,,UUUUUUUUU.WT",
            "TW.GGGGGGGGGGG,....oooooo<<<<<oooooo.......,UUUUUUUUU.WT",
            "TW+GGGGGGGGGGG,....oooooooooooo^oooo.......,WWwWWWwWW+WT",
            "TW.wwwwwwwwwww,.....oooo^ooo^oooooo........,WWWWDWWWW.WT",
            "TW.wwwwwDwwwww,......ooooooooooooo..........ooooooooo.WT",
            "TW..........,........pooooooooooop..........ooooooooo.WT",
            "TW..........,..h.......ooooooooo............o!ooooo!o.WT",
            "TW..........,.....*........oo...........#############.WT",
            "TW+.......##,##............oo...........#.....#.....#+WT",
            "TW......#...,...#........l.oo.l.........#.###.#.###.#.WT",
            "TW.....#..*****..#NaNNaN...oo...~~~~....#.#...#...#.#.WT",
            "TW.......*******...........oo.j~~~~~~q..#.#.#####.#.#.WT",
            "TW..*.#..***$***...,,,,,,,,oo.l~~~~~~j..#...#.q.#...#.WT",
            "TW.......*******...........oo...~~~~....###.#.#.#.###.WT",
            "TW+....#..*****..#NaNNaN...oo...........#...#.#.....#+WT",
            "TW......#.......#........l.oo.l...j.....#.#####.###.#.WT",
            "TW........#####............oo...........#...........#.WT",
            "TW.........................oo...........######.######.WT",
            "TW..+.....+.....+.....+....oo,,,,,,,,,,,,,,,,,,.....+.WT",
            "TWWWWWWWWWWWWWWWWWWWWWWWWWWooWWWWWWWWWWWWWWWWWWWWWWWWWWT",
            "T.t..t..t...ttt..t....tt.tloolt...t.tt....tttt.t.t..tt.T",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTooTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        // Act II: Professor Oakes's glasshouse.
        public static readonly string[] Glasshouse =
        {
            "WwwwwwwwwwwwwwwwwwwW",
            "w:pp:vvv:::vvv::pp:w",
            "w::::vvv:::vvv:::::w",
            "w::::::::qq::::::::w",
            "w:vvv::::::::::vvv:w",
            "w:vvv::pp::pp::vvv:w",
            "w::::::::::::::::::w",
            "w:pp:vvv::::vvv:pp:w",
            "w::::vvv::::vvv::::w",
            "w::::::::::::::::::w",
            "w::x:::::::::::-:::w",
            "WwwwwwwwwDwwwwwwwwwW",
        };

        // Act II: Magister Varrow's observatory.
        public static readonly string[] Observatory =
        {
            "WWWWWWWWWWWWWWWW",
            "Wzz:::!::!:::zzW",
            "W::::::::::::::W",
            "W:-:::<<<<:::-:W",
            "W::::::<&<:::::W",
            "W:::::<<<<:::::W",
            "W::::::::::::::W",
            "W:a::::::::::a:W",
            "W::::::::::::::W",
            "Wzz::::::::::zzW",
            "WWWWWWWDWWWWWWWW",
        };

        // Act II: the Library of Quiet Stars. Row 2 is the Endless Stacks (loop warp at 29,2); the warded door is (33,2); the way out (17,23).
        public static readonly string[] Library =
        {
            "WWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWW",
            "WzzzzzzzzzzzzzzqzqzzzzzzzzzzzzzqzW",
            "W::::::::::::::::::::::::::::::::D",
            "W::zzzzzzzzzzzzzzzzzzzzzzzzzzzzqzW",
            "W::::::::::::::::::::::::::::::::W",
            "W:zz::zz::zz::zz::zz::zz::zz::zz:W",
            "W:zz::zz::zz::zz::zz^:zz::zz::zz:W",
            "W:zz^:zz::zz::zz::zz::zz::zz::zz:W",
            "W:zz::zz::zz::zz::zz::zz::zz::zz:W",
            "W::::::::::::::::::::::::::::::::W",
            "W:zz::zz::zz::zz::zz::zz::zz^:zz:W",
            "W:zz::zz::zz^:zz::zz::zz::zz::zz:W",
            "W:zz::zz::zz::zz::zz::zz::zz::zz:W",
            "W::::::::::::::::::::::::::::::::W",
            "W::::::::^:::::::::::::::^:::::::W",
            "W::::________________________::::W",
            "W::::__a___a__-_____-__a___a_::::W",
            "W::::__________CCCCC_________::::W",
            "W::^:________________________:^::W",
            "W::::__a___a___________a___a_::::W",
            "W::::________________________::::W",
            "W::::::::::::::::::::::::::::::::W",
            "W::::::::::::::::::::::::::::::::W",
            "WWWWWWWWWWWWWWWWWDWWWWWWWWWWWWWWWW",
        };

        // Act II: the Restricted Section (dark). Door back (0,9); stairs down to the Sealing Hall (21,15).
        public static readonly string[] Restricted =
        {
            "WWWWWWWWWWWWWWWWWWWWWWWW",
            "W::::::::::::::::::::::W",
            "W:zzzz:zzzz:zzzz:zzzz::W",
            "W:zzzz:zzzz:zzzz:zzzz::W",
            "W::::::::::::::::::::::W",
            "W::::::::::::::::::::::W",
            "W:::zz:zzzz:zzzz:zzz:::W",
            "W:::zz:zzzz:zzzz:zzz:::W",
            "W::::::::::::::::::::::W",
            "D:::::^::::::::::^:::::W",
            "W::::::::::::::::::::::W",
            "W::zz::::::::::::::zz::W",
            "W::zz::::::::::::::zz::W",
            "W::zz::::::::::::::zz::W",
            "W::::::::::^:::::::::::W",
            "W::::::::::::::::::::V:W",
            "W::::::::::::::::::::::W",
            "WWWWWWWWWWWWWWWWWWWWWWWW",
        };

        // Act II: the Sealing Hall (dark). A dragon-sized rune circle around an empty dais; stairs up (12,20).
        public static readonly string[] SealingHall =
        {
            "XXXXXXXXXXXXXXXXXXXXXXXXXX",
            "X::::::::::::::::::::::::X",
            "X:^:::::+:<<<<<:+:::::^::X",
            "X:::::::<<<<<<<<<::::::::X",
            "X::::::<<<:::::<<<:::::::X",
            "X::::+<<<:::::::<<<+::x::X",
            "X:::::<<:::::::::<<:::xx:X",
            "X::::<<:::<<<<<:::<<:::::X",
            "X::::<<:::<:::<:::<<:::::X",
            "X::+:<<:::<:A:<:::<<:+:::X",
            "X::::<<:::<:::<:::<<:::::X",
            "X::::<<:::<<<<<:::<<:::::X",
            "X:::::<<:::::::::<<::::::X",
            "X::::+<<<:::::::<<<+:::::X",
            "X::::::<<<:::::<<<:::::::X",
            "X:::::::<<<<<<<<<::::::^:X",
            "X:^:::::+:<<<<<:+::::::::X",
            "X::::::::::::::::::::::::X",
            "X::--:::::::::::::::--:::X",
            "X::::::::::::::::::::::::X",
            "X:::::::::::E::::::::::::X",
            "XXXXXXXXXXXXXXXXXXXXXXXXXX",
        };

        // Act II: the Orrery Tower, High Magister Ashby's study at the top of three hundred and twelve stairs. Door (8,13).
        public static readonly string[] AshbyTower =
        {
            "WWWWWWwwwwwwWWWWWW",
            "Wzz:::::!:::::::zW",
            "Wz::::::::::::::zW",
            "W::-::::::::::-::W",
            "W:::::______:::::W",
            "W:::::__&___:::::W",
            "W:::::______:::::W",
            "W::^::::::::::^::W",
            "W::::::a:::a:::::W",
            "W:::::::::::::::zW",
            "Wzz:::::::::::::zW",
            "W::+:::::::::+:::W",
            "W::::::::::::::::W",
            "WWWWWWWWDWWWWWWWWW",
        };
    }
}
