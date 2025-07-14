using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using rnd = UnityEngine.Random;

public class arsGoetiaIdentification : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    public KMSelectable[] keyboard;
    public TextMesh[] keyTexts;
    public TextMesh screenText;
    public Renderer display;
    public Texture questionMark;
    public Texture[] symbols;
    public Renderer[] lights;
    public Material[] ledMats;

    private int stage;
    private int[] demons = new int[3];

    private static readonly string[] allNames = new[] { "Bael", "Agares", "Vassago", "Samigina", "Marbas", "Valefor", "Amon", "Barbatos", "Paimon", "Buer", "Gusion", "Sitri", "Beleth", "Leraje", "Eligos", "Zepar", "Botis", "Bathin", "Sallos", "Purson", "Marax", "Ipos", "Aim", "Naberius", "Glasya-Labolas", "Bune", "Ronove", "Berith", "Astaroth", "Forneus", "Foras", "Asmoday", "Gaap", "Furfur", "Marchosias", "Stolas", "Pheynix", "Halphas", "Malphas", "Raum", "Focalor", "Vephar", "Sabnock", "Shaz", "Vinea", "Bifrovs", "Voval", "Haagenti", "Crocell", "Furcas", "Balaam", "Alloces", "Camio", "Murmur", "Orobas", "Gremory", "Voso", "Avnas", "Oriax", "Naphula", "Zagan", "Ualac", "Andras", "Flauros", "Andrealphus", "Cimejes", "Amdusias", "Belial", "Decarabia", "Seere", "Dantalion", "Andromalius" };
    private string[] names;
    private static readonly KeyCode[] typeableKeys = new[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0, KeyCode.Minus, KeyCode.Equals, KeyCode.Backspace, KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P, KeyCode.LeftBracket, KeyCode.RightBracket, KeyCode.Backslash, KeyCode.CapsLock, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.Semicolon, KeyCode.Quote, KeyCode.Return, KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M, KeyCode.Comma, KeyCode.Period, KeyCode.Slash, KeyCode.Space, KeyCode.LeftShift, KeyCode.RightShift };

    private bool capsLock;
    private bool active;
    private bool activated;
    private bool moduleSelected;
    private bool backspacePressed;
    private bool backspaceHeld;
    private bool shiftHeld;
    private Coroutine holdDeleting;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    private void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable key in keyboard)
            key.OnInteract += delegate () { KeyPress(key); return false; };
        keyboard[12].OnInteract += delegate () { backspacePressed = true; return false; };
        keyboard[12].OnInteractEnded += delegate () { if (!Input.GetKey(KeyCode.Backspace) && holdDeleting != null) { StopCoroutine(holdDeleting); holdDeleting = null; } backspacePressed = false; };
        keyboard[50].OnInteractEnded += delegate () { Caps(); shiftHeld = false; };
        module.OnActivate += delegate () { activated = true; };
        var ixsOfNames = Enumerable.Range(0, symbols.Length).Select(i => Array.IndexOf(allNames, symbols[i].name)).ToArray();
        names = ixsOfNames.OrderBy(x => Array.IndexOf(allNames, ixsOfNames[x])).Select(i => allNames[i]).ToArray();
        var mainSelectable = GetComponent<KMSelectable>();
        mainSelectable.OnFocus += delegate () { moduleSelected = true; };
        mainSelectable.OnDefocus += delegate () { moduleSelected = false; };
        demons = Enumerable.Range(0, allNames.Length).ToList().Shuffle().Take(3).ToArray();
    }

    private void KeyPress(KMSelectable key)
    {
        key.AddInteractionPunch(.25f);
        audio.PlaySoundAtTransform("type", key.transform);
        if (!activated)
            return;
        var ix = Array.IndexOf(keyboard, key);
        switch (ix)
        {
            case 38:
                PressEnter();
                break;
            case 26:
                Caps();
                break;
            case 12:
                if (screenText.text.Length == 0)
                    return;
                screenText.text = screenText.text.Substring(0, screenText.text.Length - 1);
                if (holdDeleting == null)
                    holdDeleting = StartCoroutine(Backspace());
                break;
            case 49:
                if (CanAddCharacter(' '))
                    screenText.text += " ";
                break;
            case 50:
                shiftHeld = true;
                Caps();
                break;
            default:
                if (CanAddCharacter(keyTexts[ix].text.ToCharArray()[0]))
                    screenText.text += keyTexts[ix].text;
                break;
        }
    }

    private void PressEnter()
    {
        if (moduleSolved)
            return;
        if (!active)
        {
            active = true;
            display.material.mainTexture = symbols[demons[stage]];
            audio.PlaySoundAtTransform("sound" + rnd.Range(1, 9), transform);
            Debug.LogFormat("[Ars Goetia Identification #{0}] Stage {1}: You need to submit {2}.", moduleId, stage + 1, names[demons[stage]]);
        }
        else
        {
            Debug.LogFormat("[Ars Goetia Identification #{0}] You submitted {1}.", moduleId, screenText.text);
            if (screenText.text == names[demons[stage]])
            {
                Debug.LogFormat("[Ars Goetia Identification #{0}] That was correct.", moduleId);
                stage++;
                lights[stage - 1].material = ledMats[1];
                if (stage == 3)
                {
                    module.HandlePass();
                    moduleSolved = true;
                    Debug.LogFormat("[Ars Goetia Identification #{0}] Module solved!", moduleId);
                    StartCoroutine(SolveText());
                    audio.PlaySoundAtTransform("solve", transform);
                    display.gameObject.SetActive(false);
                    StartCoroutine(FlashLights());
                }
                else
                {
                    screenText.text = "";
                    display.material.mainTexture = questionMark;
                    active = false;
                    audio.PlaySoundAtTransform("stage", transform);
                }
            }
            else
            {
                module.HandleStrike();
                Debug.LogFormat("[Ars Goetia Identification #{0}] That was incorrect.", moduleId);
            }
        }
    }

    private IEnumerator SolveText()
    {
        StartCoroutine(ProcessTwitchCommand("clear"));
        yield return new WaitUntil(() => screenText.text.Length == 0);
        StartCoroutine(ProcessTwitchCommand("type In nomine dei nostri Luciferi excelsi."));
    }

    private IEnumerator FlashLights()
    {
        for (int i = 0; i < 5; i++)
        {
            foreach (Renderer l in lights)
                l.material = ledMats[2];
            yield return new WaitForSeconds(i == 0 ? .75f : .25f);
            foreach (Renderer l in lights)
                l.material = ledMats[0];
            yield return new WaitForSeconds(i == 4 ? 1f : .25f);
        }
        foreach (Renderer l in lights)
            l.material = ledMats[1];
    }

    private void Update()
    {
        backspaceHeld = Input.GetKey(KeyCode.Backspace) || backspacePressed;
        if (moduleSelected || Application.isEditor)
        {
            foreach (KeyCode key in typeableKeys)
                if (Input.GetKeyDown(key))
                    KeyPress(keyboard[Array.IndexOf(typeableKeys, key) == 51 ? 50 : Array.IndexOf(typeableKeys, key)]);
        }
        if ((Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift)) && shiftHeld)
        {
            Caps();
            shiftHeld = false;
        }
        if (Input.GetKeyUp(KeyCode.Backspace) && holdDeleting != null && !backspacePressed)
        {
            StopCoroutine(holdDeleting);
            holdDeleting = null;
        }
    }

    private bool CanAddCharacter(char c)
    {
        var origRotation = screenText.transform.localRotation;
        var origScale = screenText.transform.localScale;
        var origText = screenText.text;

        screenText.transform.parent = null;
        screenText.transform.localEulerAngles = new Vector3(90, 0, 0);
        screenText.transform.localScale = new Vector3(1, 1, 1);

        screenText.text = origText + c;
        var size = screenText.GetComponent<MeshRenderer>().bounds.size;

        screenText.transform.parent = transform.Find("Text Box");
        screenText.transform.localRotation = origRotation;
        screenText.transform.localScale = origScale;
        screenText.text = origText;
        return size.x < .295f;
    }

    private void Caps()
    {
        foreach (TextMesh t in keyTexts)
            if (t.text != "Enter" && t.text != "Backspace" && t.text != "Caps Lock" && t.text != "Shift")
                t.text = capsLock ? t.text.ToLowerInvariant() : t.text.ToUpperInvariant();
        capsLock = !capsLock;
    }

    private IEnumerator Backspace()
    {
        yield return new WaitForSeconds(.5f);
        while (backspaceHeld)
        {
            if (screenText.text.Length != 0)
            {
                screenText.text = screenText.text.Substring(0, screenText.text.Length - 1);
                audio.PlaySoundAtTransform("type", keyboard[12].transform);
            }
            yield return new WaitForSeconds(.05f);
        }
    }

    //Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} enter/submit <text> to submit <text> as answer. The module will just press enter if <text> is empty. | Use !{0} type <text> to type <text> into the module.| Use !{0} backspace <number> to press backspace <number> times. Limited to 2 digits number at most. | Use !{0} clear to clear the text from the module.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string cmd)
    {
        yield return null;
        var keyboardButCapsLock = @"QWERTYUIOPASDFGHJKLZXCVBNM";
        cmd = cmd.Trim();
        Match m = Regex.Match(cmd, @"^(?:(enter|submit)(?: ([ -~]{1,50}))?|type ([ -~]{1,50})|backspace (\d?\d)|clear)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success)
        {
            yield return null;
            if (m.Groups[1].Success)
            {
                if (!m.Groups[2].Success)
                {
                    keyboard[38].OnInteract();
                    yield return new WaitForSeconds(.025f);
                    yield break;
                }
                while (screenText.text.Length > m.Groups[2].Value.Length || (screenText.text != "" && screenText.text != m.Groups[2].Value.Substring(0, screenText.text.Length)))
                {
                    keyboard[12].OnInteract();
                    yield return new WaitForSeconds(.025f);
                    keyboard[12].OnInteractEnded();
                    yield return "trycancel";
                }
                int initialLength = screenText.text.Length;
                foreach (char c in m.Groups[2].Value.Substring(initialLength))
                {
                    if (c == ' ')
                    {
                        keyboard[49].OnInteract();
                        yield return new WaitForSeconds(.025f);
                        yield return "trycancel";
                        continue;
                    }
                    if (keyboardButCapsLock.Contains(c) ^ capsLock)
                    {
                        keyboard[26].OnInteract();
                        yield return new WaitForSeconds(.025f);
                        yield return "trycancel";
                    }
                    keyboard[Array.IndexOf(keyTexts.Select(x => x.text).ToArray(), c.ToString())].OnInteract();
                    yield return new WaitForSeconds(.025f);
                    yield return "trycancel";
                }
                keyboard[38].OnInteract();
                yield return new WaitForSeconds(.025f);
            }
            else if (m.Groups[3].Success)
                foreach (char c in m.Groups[3].Value)
                {
                    if (c == ' ')
                    {
                        keyboard[49].OnInteract();
                        yield return new WaitForSeconds(.025f);
                        yield return "trycancel";
                        continue;
                    }
                    if (keyboardButCapsLock.Contains(c) ^ capsLock)
                    {
                        keyboard[26].OnInteract();
                        yield return new WaitForSeconds(.025f);
                        yield return "trycancel";
                    }
                    keyboard[Array.IndexOf(keyTexts.Select(x => x.text).ToArray(), c.ToString())].OnInteract();
                    yield return new WaitForSeconds(.025f);
                    yield return "trycancel";
                }
            else if (m.Groups[12].Success)
                for (int i = 0; i < int.Parse(m.Groups[4].Value); i++)
                {
                    keyboard[13].OnInteract();
                    yield return new WaitForSeconds(.025f);
                    yield return "trycancel";
                }
            else
                while (screenText.text != "")
                {
                    keyboard[12].OnInteract();
                    yield return new WaitForSeconds(.025f);
                    keyboard[12].OnInteractEnded();
                    yield return "trycancel";
                }
        }
        else
            yield return "sendtochaterror Invalid command! Valid commands are enter/submit, type, backspace, and clear. Use !{1} help for full command.";
        yield break;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        while (!moduleSolved)
        {
            if (!active) yield return ProcessTwitchCommand("enter");
            yield return ProcessTwitchCommand("clear");
            yield return ProcessTwitchCommand("enter " + names[demons[stage]]);
        }
    }
}
