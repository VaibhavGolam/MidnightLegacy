using System.Collections.Generic;

namespace MidnightLegacy
{
    public sealed class DialogueChoice
    {
        public string text;
        public string go;
        public string[] fx;
    }

    public sealed class DialogueNode
    {
        public string label;
        public string speaker;
        public string text;
        public string[] fx;
        public DialogueChoice[] choices;
        public string jump;

        public DialogueNode L(string l) { label = l; return this; }
        public DialogueNode J(string j) { jump = j; return this; }
        public DialogueNode Fx(params string[] f) { fx = f; return this; }
    }

    public sealed class DialogueDef
    {
        public string id;
        public string bg;
        public string mood;
        public readonly List<DialogueNode> nodes = new List<DialogueNode>();

        public int IndexOf(string label)
        {
            for (int i = 0; i < nodes.Count; i++) if (nodes[i].label == label) return i;
            return -1;
        }
    }

    public enum StepKind { Dialogue, Race }

    public sealed class StoryStep
    {
        public string id;
        public StepKind kind;
        public string refId;
        public string title;
        public int chapter;
        public int minRep;
        public string hint;
    }

    /// <summary>
    /// The story: three chapters of dialogue and races. Effects on a node are strings:
    /// flag:name, rep:+10, cash:+200, stage:uncover. Text may contain {name} for the player's name.
    /// </summary>
    public static class StoryContent
    {
        public static readonly Dictionary<string, DialogueDef> Dialogues = new Dictionary<string, DialogueDef>();
        public static readonly List<StoryStep> Steps = new List<StoryStep>();
        static bool ready;

        public static DialogueDef Dialogue(string id)
        {
            DialogueDef d;
            return id != null && Dialogues.TryGetValue(id, out d) ? d : null;
        }

        // ---- tiny script helpers ----
        static DialogueNode N(string speaker, string text) { return new DialogueNode { speaker = speaker, text = text }; }

        static DialogueNode Q(string speaker, string text, params DialogueChoice[] choices)
        {
            return new DialogueNode { speaker = speaker, text = text, choices = choices };
        }

        static DialogueChoice C(string text, string go, params string[] fx)
        {
            return new DialogueChoice { text = text, go = go, fx = fx };
        }

        static void Dlg(string id, string bg, string mood, params DialogueNode[] nodes)
        {
            DialogueDef d = new DialogueDef { id = id, bg = bg, mood = mood };
            d.nodes.AddRange(nodes);
            Dialogues[id] = d;
        }

        static void Step(string id, StepKind kind, string refId, string title, int chapter, int minRep, string hint)
        {
            Steps.Add(new StoryStep { id = id, kind = kind, refId = refId, title = title, chapter = chapter, minRep = minRep, hint = hint });
        }

        public static void Init()
        {
            if (ready) return;
            ready = true;
            BuildChapter1();
            BuildChapter2();
            BuildChapter3();
            BuildSteps();
        }

        static void BuildSteps()
        {
            Step("s_prologue", StepKind.Dialogue, "d_prologue", "Prologue: The Key", 1, 0, null);
            Step("s_garage", StepKind.Dialogue, "d_garage", "Cinder Lane", 1, 0, null);
            Step("s_tutorial", StepKind.Race, "tutorial", "Harbor Lap", 1, 0, null);
            Step("s_sato", StepKind.Dialogue, "d_sato_intro", "The Ghost", 1, 0, null);
            Step("s_first", StepKind.Race, "ch1_first", "First Night", 1, 0, null);
            Step("s_unknown", StepKind.Dialogue, "d_unknown_msg", "Unknown Number", 1, 0, null);

            Step("s_c2_open", StepKind.Dialogue, "d_c2_open", "Chapter 2: Rookie Night", 2, 15, "Earn reputation in side events.");
            Step("s_c2_coast", StepKind.Race, "ch2_coast", "Coast Time Attack", 2, 15, null);
            Step("s_c2_city", StepKind.Race, "ch2_city", "Outskirts Slalom", 2, 30, "Reach 30 reputation.");
            Step("s_c2_mid", StepKind.Dialogue, "d_c2_mid", "Redline", 2, 30, null);
            Step("s_c2_rain", StepKind.Race, "ch2_rain", "Rain Rematch", 2, 45, "Reach 45 reputation.");
            Step("s_c2_escape", StepKind.Race, "ch2_escape", "Redline Escape", 2, 60, "Reach 60 reputation.");
            Step("s_c2_end", StepKind.Dialogue, "d_c2_end", "The Black Car", 2, 60, null);

            Step("s_c3_open", StepKind.Dialogue, "d_c3_open", "Chapter 3: The Black Coupe", 3, 90, "Reach 90 reputation.");
            Step("s_c3_chase", StepKind.Race, "ch3_chase", "Catch the Black Coupe", 3, 100, "Reach 100 reputation.");
            Step("s_c3_final", StepKind.Race, "ch3_final", "Kairo Pass, Midnight", 3, 120, "Reach 120 reputation.");
        }

        // ------------------------------------------------------------------------------------------

        static void BuildChapter1()
        {
            Dlg("d_prologue", "room", "calm",
                N("narr", "Port Alder. Three weeks after the funeral."),
                N("jun", "The apartment feels smaller every day. Or I'm just tired of it."),
                N("narr", "A brass key, a folded letter and an email from a lawyer. That is everything my grandfather left me."),
                N("ghost", "Jun. The garage on Cinder Lane is yours. So is what is under the cover. Don't sell her. Don't let her rot either."),
                N("jun", "He never mentioned a car. He barely mentioned anything."),
                Q("jun", "What do I do with it?",
                    C("Go and look tonight.", "go_look", "flag:brave"),
                    C("Sell it. Rent is due.", "go_sell", "flag:practical")),
                N("jun", "No point waiting. Midnight is as good a time as any.").L("go_look").J("end"),
                N("jun", "Rent is due in a week. I'll see what it's worth, at least.").L("go_sell"),
                N("narr", "Cinder Lane. 11:48 PM.").L("end"));

            Dlg("d_garage", "garage", "calm",
                N("narr", "The shutter fights back for a full minute before it gives."),
                N("jun", "Smells like oil and old paper."),
                N("narr", "Under a dust sheet, something low and boxy is waiting."),
                N("jun", "Here goes.").Fx("stage:uncover"),
                N("jun", "A white coupe. Black trim. Pop up headlights."),
                N("jun", "Grandpa, what were you hiding?"),
                N("sato", "Don't touch the left mirror. It has been loose since 1998."),
                N("jun", "Who are you?"),
                N("sato", "Sato. I kept her running for your grandfather for thirty years. He told me you would come, so I kept the spare key."),
                N("sato", "The Kairo Hachi-R. Rear wheel drive, one owner, no nonsense. She has been waiting for someone to drive her properly."),
                Q("jun", "Why does everyone say properly?",
                    C("Why 'properly'?", "proper"),
                    C("Is she fast?", "fast")),
                N("sato", "Because he only ever drove her one way. Hard, clean, and at night.").L("proper").J("after"),
                N("sato", "Fast enough that the Pass crowd still talks about a white car with no plate.").L("fast"),
                N("sato", "Get in. Once around the harbor, nothing more. I will tell you what she wants.").L("after").Fx("flag:met_sato"),
                N("jun", "One lap."));

            Dlg("d_sato_intro", "garage", "calm",
                N("sato", "Not bad. You panic a little less every corner."),
                N("jun", "You said he drove at night. Why?"),
                N("sato", "Because that is when Kairo Pass wakes up. They called him the Ghost. White car, lights off until the last corner, gone before anyone could read a plate."),
                N("jun", "The Ghost. That was Grandpa?"),
                N("sato", "Twenty years ago he walked away. Nobody got a reason out of him. Not even me."),
                Q("jun", "What now?",
                    C("Then I'll find out why.", "resolve", "flag:resolve"),
                    C("I don't want any of this.", "doubt", "flag:doubt")),
                N("sato", "Good. Then you start where he started. Harbor Road. A kid called Rook races anything with a pulse.").L("resolve").J("end"),
                N("sato", "Nobody is forcing you. But a car like this needs to be driven, and Rook wants a race either way.").L("doubt"),
                N("jun", "One race. Just to see.").L("end"),
                N("sato", "Midnight, the old harbor road. Do not crash my car."));

            Dlg("d_first_before", "road", "tense",
                N("rook", "White coupe, black trim. Nobody has seen that car in twenty years."),
                N("rook", "So who is driving it? A college kid?"),
                N("jun", "My name is {name}."),
                N("rook", "Rook. Two minutes to the line, then I find out if you earned that car."),
                Q("jun", "Your reply?",
                    C("I didn't earn it. Yet.", "humble", "flag:rook_respect"),
                    C("Watch my taillights.", "cocky")),
                N("rook", "Honest. I like that.").L("humble").J("end"),
                N("rook", "Big words from someone in a borrowed car.").L("cocky"),
                N("narr", "Engines idle. The harbor lights shake on the water.").L("end"));

            Dlg("d_first_win", "road", "calm",
                N("rook", "Okay. Okay! That was fast."),
                N("rook", "Not Ghost fast. But something moved in that car tonight."),
                N("jun", "Did you see it on the long straight?"),
                N("rook", "See what?"),
                N("narr", "A black coupe, no lights, a masked driver. Gone before the corner."),
                N("jun", "Never mind.").Fx("flag:saw_black_car"),
                N("rook", "Come to the Pass on Friday. People will want to meet whoever is driving the Ghost's car."));

            Dlg("d_first_lose", "road", "calm",
                N("rook", "Close. Not close enough."),
                N("jun", "Again."),
                N("rook", "Sure. But you lifted in the second corner. Brake before it, then let her rotate. The car wants you to keep going."));

            Dlg("d_unknown_msg", "phone", "tense",
                N("narr", "11:52 PM. An unknown number."),
                N("unknown", "You drive like him."),
                N("unknown", "Twenty years ago he left a race unfinished. I came to finish it."),
                N("unknown", "Kairo Pass. When you are ready."),
                N("jun", "Who is this?"),
                N("narr", "No answer. The chat shows only an icon of a black coupe.").Fx("flag:got_message_unknown"),
                N("narr", "Chapter 1 complete."));
        }

        static void BuildChapter2()
        {
            Dlg("d_c2_open", "garage", "calm",
                N("mia", "Okay, so I told everyone at Alder Tech that my best friend inherited a legend."),
                N("jun", "Mia, no."),
                N("mia", "Too late. There is a time attack on the coast. Sato says the car loves cold air."),
                N("sato", "Reputation opens doors. Win a few races, spend the money on parts, and learn what she does when she is pushed."),
                N("mia", "I will run comms. You run fast.").Fx("flag:mia_ally"));

            Dlg("d_c2_coast_win", "road", "calm",
                N("mia", "Sixty seconds on the clock and you made it look easy."),
                N("jun", "I was terrified."),
                N("mia", "Terrified looks good on you. Keep it."));

            Dlg("d_c2_mid", "road", "tense",
                N("mia", "The Outskirts run was clean. Traffic everywhere, no scratches."),
                N("sato", "Thinking ahead. That is your grandfather's habit."),
                N("mia", "Bad news. Rook is racing again, but not as a friend. The Redline Syndicate got to him."),
                N("jun", "Redline?"),
                N("sato", "Dex runs them. They do not race for the road. They race for the pot."));

            Dlg("d_rook_rematch_before", "road", "tense",
                N("rook", "Rain suits you. I have new tires."),
                N("jun", "Redline paid for them?"),
                N("rook", "It does not matter. I need this win."),
                Q("jun", "Your reply?",
                    C("You don't have to do this.", "soft", "flag:reached_rook"),
                    C("Then let's race.", "hard")),
                N("rook", "I do.").L("soft").J("end"),
                N("rook", "Finally.").L("hard"),
                N("narr", "The rain gets heavier. The road shines like a river.").L("end"));

            Dlg("d_rook_rematch_win", "road", "calm",
                N("rook", "You were better. Again."),
                N("rook", "Dex will not be happy. Watch your mirrors tonight."));

            Dlg("d_rook_rematch_lose", "road", "calm",
                N("rook", "Not your night. Rain punishes anyone who rushes."),
                N("rook", "Try again. Lift earlier, brake straight, and let the car turn."));

            Dlg("d_c2_escape_before", "city", "tense",
                N("dex", "You beat my driver. Fine. Let us see if you can outrun my enforcer."),
                N("jun", "This is a race?"),
                N("dex", "This is a lesson."),
                N("narr", "Headlights flare in the mirror. A dark red coupe is closing fast."));

            Dlg("d_c2_end", "garage", "calm",
                N("dex", "Respect. The Pass is open to you now."),
                N("mia", "Jun. The black car texted me. Asked for your number. I did not give it."),
                N("jun", "He already has it."),
                N("sato", "Then it is time you saw what is in the back of the garage."),
                N("narr", "Chapter 2 complete."));
        }

        static void BuildChapter3()
        {
            Dlg("d_c3_open", "garage", "tense",
                N("sato", "Your grandfather kept this. One page for every year he did not race."),
                N("jun", "The last night on Kairo Pass, I was winning. Then the black car lost its line. I lifted. He crashed. He lived. I never raced again."),
                N("jun", "He lifted for the other driver. He gave away the win."),
                N("sato", "The black coupe belonged to Hiroshi Aoki. He survived, but he never drove again."),
                N("jun", "And the Unknown Driver?"),
                N("sato", "I think you will find out tonight.").Fx("flag:read_notebook"));

            Dlg("d_c3_chase_before", "pass", "tense",
                N("unknown", "You came."),
                N("jun", "Who are you?"),
                N("unknown", "Catch me and I will tell you."),
                N("narr", "The fog swallows the first corner. A black coupe is already gone."));

            Dlg("d_c3_chase_win", "pass", "tense",
                N("unknown", "Not bad. Not him. But not bad."),
                N("jun", "Why are you doing this?"),
                N("unknown", "Because he left. Because someone had to finish."),
                N("unknown", "Midnight. Kairo Pass. The real race."));

            Dlg("d_c3_chase_lose", "pass", "tense",
                N("unknown", "Too slow. He would have caught me by the second bend."),
                N("jun", "Then I'll try again."));

            Dlg("d_c3_final_before", "pass", "tense",
                N("narr", "The summit. Fog rolling in. Two cars on the line."),
                N("unknown", "My father taught me this road. Your grandfather made him stop driving it."),
                Q("jun", "Your reply?",
                    C("He saved your father's life.", "truth", "flag:truth"),
                    C("I'm not him.", "own", "flag:own_path")),
                N("unknown", "He took my father's career.").L("truth").J("end"),
                N("unknown", "Then show me who you are.").L("own"),
                N("narr", "Lights off. Engines on.").L("end"));

            Dlg("d_c3_final_win", "pass", "calm",
                N("narr", "The black coupe pulls over. The driver takes off the mask."),
                N("rina", "I hated him for twenty years. Then I read his letters to my father."),
                N("jun", "He wrote to him?"),
                N("rina", "Every year. Dad never answered. He couldn't."),
                N("sato", "Both of them were stubborn."),
                N("rina", "I came to finish a race. Turns out it was already finished the night he lifted."),
                N("jun", "Legacy is inherited. Greatness is earned."),
                N("rina", "Come back to the Pass. Not as the Ghost's grandson. As yourself.").Fx("flag:legend_unlocked", "flag:story_complete"),
                N("narr", "Legend Mode unlocked."));

            Dlg("d_c3_final_lose", "pass", "tense",
                N("unknown", "Again. I will wait."));
        }
    }
}
