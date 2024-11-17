using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Less3.Terminal.Editor
{
    public struct TerminalEntry
    {
        public string message;
        public string stackTrace;
        public int frame;
        public System.DateTime time;
        public TerminalEntryType type;
        public bool isCompilationLog;
    }

    public enum TerminalEntryType
    {
        Log,
        Warning,
        Error,
        Command,
        CommandOut,
    }

    public static class TerminalWindowSettings
    {
        private static string SHOW_LOGS = "TerminalWindow.ShowLogs";
        public static bool showLogs
        {
            get => EditorPrefs.GetBool(SHOW_LOGS, true);
            set => EditorPrefs.SetBool(SHOW_LOGS, value);
        }

        private static string SHOW_WARNINGS = "TerminalWindow.ShowWarnings";
        public static bool showWarnings
        {
            get => EditorPrefs.GetBool(SHOW_WARNINGS, true);
            set => EditorPrefs.SetBool(SHOW_WARNINGS, value);
        }

        private static string SHOW_ERRORS = "TerminalWindow.ShowErrors";
        public static bool showErrors
        {
            get => EditorPrefs.GetBool(SHOW_ERRORS, true);
            set => EditorPrefs.SetBool(SHOW_ERRORS, value);
        }

        private static string SHOW_COMMANDS = "TerminalWindow.ShowCommands";
        public static bool showCommands
        {
            get => EditorPrefs.GetBool(SHOW_COMMANDS, true);
            set => EditorPrefs.SetBool(SHOW_COMMANDS, value);
        }

        // 0 = none, 1 = time, 2 = frame
        private static string TIMESTAMP_MODE = "TerminalWindow.TimestampMode";
        public static int timestampMode
        {
            get => EditorPrefs.GetInt(TIMESTAMP_MODE, 1);
            set => EditorPrefs.SetInt(TIMESTAMP_MODE, value);
        }

        // 0 = 10, 1 = 10.5, 2 = 11, 3 = 12
        private static string TEXT_SIZE = "TerminalWindow.TextSize";
        public static int textSize
        {
            get => EditorPrefs.GetInt(TEXT_SIZE, 0);
            set => EditorPrefs.SetInt(TEXT_SIZE, value);
        }
        public static float GetTextSizeFloat()
        {
            switch (textSize)
            {
                case 0:
                    return 10;
                case 1:
                    return 10.5f;
                case 2:
                    return 11f;
                case 3:
                    return 12f;
                default:
                    return 10f;
            }
        }
        public static int GetLineHeight()
        {
            switch (textSize)
            {
                case 0:
                    return 16;
                case 1:
                    return 17;
                case 2:
                    return 18;
                case 3:
                    return 20;
                default:
                    return 16;
            }
        }
    }

    public class TerminalWindow : EditorWindow
    {
        public VisualTreeAsset windowAsset;
        public VisualTreeAsset entryAsset;
        public Texture iconTexture;

        private ListView listView;
        private ScrollView scrollView;
        private VisualElement terminalRoot;
        private VisualElement stackTraceRoot;

        private TextField commandField;
        private VisualElement enterCommandHint;
        private VisualElement carot;

        private static List<TerminalEntry> allEntries = new List<TerminalEntry>();
        private static List<TerminalEntry> filteredEntries = new List<TerminalEntry>();

        private Label stackTraceName;
        private Label stackTraceText;
        private Button JumpToNowButton;

        [MenuItem("Window/General/Terminal")]
        public static void ShowExample()
        {
            TerminalWindow wnd = GetWindow<TerminalWindow>();
            wnd.titleContent = new GUIContent("Terminal", wnd.iconTexture);
        }

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
            TerminalManager.OnCommandInputOut += HandleCommandInput;
            TerminalManager.OnCommandLogOut += HandleCommandOutput;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
            TerminalManager.OnCommandInputOut -= HandleCommandInput;
            TerminalManager.OnCommandLogOut -= HandleCommandOutput;
        }

        private TerminalEntryType GetEntryType(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    return TerminalEntryType.Error;
                case LogType.Warning:
                    return TerminalEntryType.Warning;
                case LogType.Log:
                    return TerminalEntryType.Log;
                default:
                    return TerminalEntryType.Log;
            }
        }

        private void HandleCommandInput(string command, string description)
        {
            TerminalEntry entry = new TerminalEntry
            {
                message = command,
                stackTrace = description,
                frame = Time.frameCount,
                time = System.DateTime.Now,
                type = TerminalEntryType.Command
            };
            allEntries.Add(entry);
            FilterNewEntry(entry);

            bool scrollTo = false;
            if (scrollView != null && scrollView.verticalScroller != null)
            {
                scrollTo = scrollView.verticalScroller.value == scrollView.verticalScroller.highValue || scrollView.verticalScroller.highValue < 0;
            }

            if (listView != null)
            {
                listView.Rebuild();
            }

            if (scrollTo)
            {
                scrollView.verticalScroller.ScrollPageDown(9999999f);
            }
        }

        private void HandleCommandOutput(string command, string description)
        {
            TerminalEntry entry = new TerminalEntry
            {
                message = command,
                stackTrace = description,
                frame = Time.frameCount,
                time = System.DateTime.Now,
                type = TerminalEntryType.CommandOut
            };
            allEntries.Add(entry);
            FilterNewEntry(entry);
            bool scrollTo = false;
            if (scrollView != null && scrollView.verticalScroller != null)
            {
                scrollTo = scrollView.verticalScroller.value == scrollView.verticalScroller.highValue || scrollView.verticalScroller.highValue < 0;
            }

            if (listView != null)
            {
                listView.Rebuild();
            }

            if (scrollTo)
            {
                scrollView.verticalScroller.ScrollPageDown(9999999f);
            }
        }

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            TerminalEntry entry = new TerminalEntry
            {
                message = message,
                stackTrace = stackTrace,
                frame = Time.frameCount,
                time = System.DateTime.Now,
                type = GetEntryType(type),
                isCompilationLog = EditorApplication.isCompiling,
            };
            allEntries.Add(entry);
            FilterNewEntry(entry);

            bool scrollTo = false;
            if (scrollView != null && scrollView.verticalScroller != null)
            {
                scrollTo = scrollView.verticalScroller.value == scrollView.verticalScroller.highValue || scrollView.verticalScroller.highValue < 0;
            }

            if (listView != null)
            {
                listView.Rebuild();
            }

            if (scrollTo)
            {
                scrollView.verticalScroller.ScrollPageDown(9999999f);
            }
        }

        private void FilterNewEntry(TerminalEntry entry)
        {
            if (TerminalWindowSettings.showLogs && entry.type == TerminalEntryType.Log)
            {
                filteredEntries.Add(entry);
            }
            else if (TerminalWindowSettings.showWarnings && entry.type == TerminalEntryType.Warning)
            {
                filteredEntries.Add(entry);
            }
            else if (TerminalWindowSettings.showErrors && entry.type == TerminalEntryType.Error)
            {
                filteredEntries.Add(entry);
            }
            else if (TerminalWindowSettings.showCommands && entry.type == TerminalEntryType.Command)
            {
                filteredEntries.Add(entry);
            }
            else if (TerminalWindowSettings.showCommands && entry.type == TerminalEntryType.CommandOut)
            {
                filteredEntries.Add(entry);
            }
        }

        private void RefilterAllEntries()
        {
            filteredEntries.Clear();
            List<TerminalEntryType> shown = new List<TerminalEntryType>();
            if (TerminalWindowSettings.showLogs)
            {
                shown.Add(TerminalEntryType.Log);
            }
            if (TerminalWindowSettings.showWarnings)
            {
                shown.Add(TerminalEntryType.Warning);
            }
            if (TerminalWindowSettings.showErrors)
            {
                shown.Add(TerminalEntryType.Error);
            }
            if (TerminalWindowSettings.showCommands)
            {
                shown.Add(TerminalEntryType.Command);
                shown.Add(TerminalEntryType.CommandOut);
            }
            foreach (TerminalEntry entry in allEntries)
            {
                if (shown.Contains(entry.type))
                {
                    filteredEntries.Add(entry);
                }
            }
            listView.Rebuild();
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            terminalRoot = windowAsset.CloneTree();
            root.Add(terminalRoot);

            listView = terminalRoot.Q<ListView>("ListView");
            scrollView = listView.Q<ScrollView>();
            listView.makeItem = MakeItem;
            listView.bindItem = BindItem;
            listView.itemsSource = filteredEntries;
            listView.fixedItemHeight = TerminalWindowSettings.GetLineHeight();
            listView.selectionChanged += objects => SelectItem((TerminalEntry)objects.First());
            listView.itemsChosen += objects => DoubleClickItem((TerminalEntry)objects.First());

            stackTraceName = terminalRoot.Q<Label>("StackTraceName");
            stackTraceText = terminalRoot.Q<Label>("StackTraceBody");
            stackTraceText.RegisterCallback<PointerDownLinkTagEvent>(evt =>
            {
                if (evt.target is Label label)
                {
                    OnLinkClick(evt.linkID);
                }
            });

            commandField = terminalRoot.Q<TextField>("CommandField");
            commandField.selectAllOnFocus = true;
            enterCommandHint = terminalRoot.Q<VisualElement>("EnterCommandHint");
            commandField.Focus();

            commandField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == "")
                {
                    enterCommandHint.style.display = DisplayStyle.Flex;
                }
                else
                {
                    enterCommandHint.style.display = DisplayStyle.None;
                }
            });

            commandField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return)
                {
                    TerminalManager.TryCommand(commandField.value);
                    commandField.value = "";
                    commandField.Focus();
                }
            });

            VisualElement commandZone = terminalRoot.Q<VisualElement>("CommandZone");
            commandZone.AddManipulator(new Clickable(() =>
            {
                commandField.Focus();
            }));

            carot = terminalRoot.Q<VisualElement>("carot");
            if (hasFocus)
            {
                carot.AddToClassList("carotFocused");
            }

            VisualElement bg = root.Q<VisualElement>("BG");
            bg.AddManipulator(new TerminalBackgroundManipulator
            {
                OnLeftClick = () =>
                {
                    commandField.Focus();
                },
                OnRightClick = () =>
                {
                    RightClickDropdown();
                }
            });

            JumpToNowButton = terminalRoot.Q<Button>("JumpToNowButton");
            JumpToNowButton.clicked += () =>
            {
                scrollView.verticalScroller.ScrollPageDown(99999999f);
            };

            stackTraceRoot = terminalRoot.Q<VisualElement>("StackTraceParent");
            stackTraceRoot.style.display = DisplayStyle.None;
        }

        public VisualElement MakeItem()
        {
            VisualElement item = entryAsset.CloneTree();
            return item;
        }

        private void SelectItem(TerminalEntry entry)
        {
            stackTraceName.text = entry.message;
            if (string.IsNullOrEmpty(entry.stackTrace))
            {
                stackTraceText.style.display = DisplayStyle.None;
            }
            else
            {
                stackTraceText.style.display = DisplayStyle.Flex;
                stackTraceText.text = HyperlinkStacktrace(entry.stackTrace);
            }

            stackTraceRoot.style.display = DisplayStyle.Flex;
        }

        private void DoubleClickItem(TerminalEntry entry)
        {
            if (!entry.isCompilationLog)
                return;

            string path = entry.message.Split('(')[0];
            string lineNum = entry.message.Split('(')[1].Split(',')[0];

            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (obj != null)
            {
                AssetDatabase.OpenAsset(obj, int.Parse(lineNum));
            }
        }

        private void BindItem(VisualElement item, int index)
        {
            TerminalEntry entry = filteredEntries[index];
            Label text = item.Q<Label>("Text");
            text.text = entry.message;
            text.style.fontSize = TerminalWindowSettings.GetTextSizeFloat();
            text.ClearClassList();
            text.AddToClassList("entryText");
            switch (entry.type)
            {
                case TerminalEntryType.Error:
                    text.AddToClassList("entryTextError");
                    break;
                case TerminalEntryType.Warning:
                    text.AddToClassList("entryTextWarning");
                    break;
                case TerminalEntryType.Command:
                    text.AddToClassList("entryTextCommand");
                    break;
                case TerminalEntryType.CommandOut:
                    text.AddToClassList("entryTextCommandOut");
                    break;
            }
            Label timestamp = item.Q<Label>("Timestamp");
            switch (TerminalWindowSettings.timestampMode)
            {
                case 0:
                    timestamp.style.display = DisplayStyle.None;
                    break;
                case 1:
                    timestamp.style.display = DisplayStyle.Flex;
                    string time = entry.time.ToString("HH:mm:ss.fff");
                    //inster <b> at the .
                    timestamp.text = time.Substring(0, time.Length - 4) + "<b>" + time.Substring(time.Length - 4) + "</b>";
                    break;
                case 2:
                    timestamp.style.display = DisplayStyle.Flex;
                    timestamp.text = entry.frame.ToString();
                    break;
            }

            VisualElement dot = item.Q<VisualElement>("Dot");
            dot.ClearClassList();
            dot.AddToClassList("entryDot");
            switch (entry.type)
            {
                case TerminalEntryType.Error:
                    dot.AddToClassList("entryDotError");
                    break;
                case TerminalEntryType.Warning:
                    dot.AddToClassList("entryDotWarning");
                    break;
                case TerminalEntryType.Command:
                    dot.AddToClassList("entryDotCommand");
                    break;
                case TerminalEntryType.CommandOut:
                    dot.AddToClassList("entryDotCommandOut");
                    break;
            }
        }

        private int t;

        private string HyperlinkStacktrace(string stacktrace)
        {
            //Detect links like (at Assets/Scripts/MyScript.cs:12) and insert a clickable link
            string[] lines = stacktrace.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int start = line.IndexOf("(at ");
                if (start != -1)
                {
                    int end = line.IndexOf(")", start);
                    if (end != -1)
                    {
                        string path = line.Substring(start + 4, end - start - 4);
                        string[] parts = path.Split(':');
                        if (parts.Length == 2)
                        {
                            string file = parts[0];
                            int lineNum = int.Parse(parts[1]);
                            if (!string.IsNullOrEmpty(file))
                            {
                                lines[i] = line.Substring(0, start) + "(at <color=#4C7EFF><u><link=\"" + file + "%" + lineNum + "\"><a>" + file + ":" + lineNum + "</link></u></color>)";
                            }
                        }
                    }
                }
            }
            //remove last line if its empty
            if (lines[lines.Length - 1] == "")
            {
                lines = lines.Take(lines.Length - 1).ToArray();
            }

            return "<color=#FFFFFF4D>" + string.Join("\n", lines) + "</color>";
        }

        private void OnLinkClick(string link)
        {
            string path = link.Split('%')[0];
            string lineNum = link.Split('%')[1];
            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (obj != null)
            {
                AssetDatabase.OpenAsset(obj, int.Parse(lineNum));
            }
        }

        // Use the right click menu as the settings menu and some useful functions
        private void RightClickDropdown()
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Clear"), false, () =>
            {
                for (int i = allEntries.Count - 1; i >= 0; i--)
                {
                    if (allEntries[i].isCompilationLog)
                    {
                        continue;
                    }
                    allEntries.RemoveAt(i);
                }
                RefilterAllEntries();
                stackTraceRoot.style.display = DisplayStyle.None;
            });
            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Show/Logs"), TerminalWindowSettings.showLogs, () =>
            {
                TerminalWindowSettings.showLogs = !TerminalWindowSettings.showLogs;
                RefilterAllEntries();
            });
            menu.AddItem(new GUIContent("Show/Warnings"), TerminalWindowSettings.showWarnings, () =>
            {
                TerminalWindowSettings.showWarnings = !TerminalWindowSettings.showWarnings;
                RefilterAllEntries();
            });
            menu.AddItem(new GUIContent("Show/Errors"), TerminalWindowSettings.showErrors, () =>
            {
                TerminalWindowSettings.showErrors = !TerminalWindowSettings.showErrors;
                RefilterAllEntries();
            });
            menu.AddItem(new GUIContent("Show/Commands"), TerminalWindowSettings.showCommands, () =>
            {
                TerminalWindowSettings.showCommands = !TerminalWindowSettings.showCommands;
                RefilterAllEntries();
            });

            menu.AddItem(new GUIContent("Timestamp/None"), TerminalWindowSettings.timestampMode == 0, () =>
            {
                TerminalWindowSettings.timestampMode = 0;
                listView.Rebuild();
            });
            menu.AddItem(new GUIContent("Timestamp/Time"), TerminalWindowSettings.timestampMode == 1, () =>
            {
                TerminalWindowSettings.timestampMode = 1;
                listView.Rebuild();
            });
            menu.AddItem(new GUIContent("Timestamp/Frame"), TerminalWindowSettings.timestampMode == 2, () =>
            {
                TerminalWindowSettings.timestampMode = 2;
                listView.Rebuild();
            });

            menu.AddItem(new GUIContent("Text Size/Small"), TerminalWindowSettings.textSize == 0, () =>
            {
                TerminalWindowSettings.textSize = 0;
                listView.fixedItemHeight = TerminalWindowSettings.GetLineHeight();
                listView.Rebuild();
            });
            menu.AddItem(new GUIContent("Text Size/Medium"), TerminalWindowSettings.textSize == 1, () =>
            {
                TerminalWindowSettings.textSize = 1;
                listView.fixedItemHeight = TerminalWindowSettings.GetLineHeight();
                listView.Rebuild();
            });
            menu.AddItem(new GUIContent("Text Size/Large"), TerminalWindowSettings.textSize == 2, () =>
            {
                TerminalWindowSettings.textSize = 2;
                listView.fixedItemHeight = TerminalWindowSettings.GetLineHeight();
                listView.Rebuild();
            });
            menu.AddItem(new GUIContent("Text Size/Extra Large"), TerminalWindowSettings.textSize == 3, () =>
            {
                TerminalWindowSettings.textSize = 3;
                listView.fixedItemHeight = TerminalWindowSettings.GetLineHeight();
                listView.Rebuild();
            });

            menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
        }

        private void OnFocus()
        {
            if (carot != null)
            {
                carot.AddToClassList("carotFocused");
            }

            if (commandField != null)
            {
                commandField.style.display = DisplayStyle.Flex;
                commandField.Focus();
            }
        }

        private void OnBecameVisible()
        {
            Focus();
        }

        private void OnLostFocus()
        {
            if (carot != null)
            {
                carot.RemoveFromClassList("carotFocused");
            }

            // Fixing some bad styles on unitys part. A focused textfield will stay visually focused with a text cursor when the window is unfocused.
            if (commandField != null)
            {
                if (commandField.value == "")
                {
                    commandField.value = "";
                    commandField.style.display = DisplayStyle.None;
                }
            }
        }

        private void Update()
        {
            JumpToNowButton.style.display = (scrollView.verticalScroller.value == scrollView.verticalScroller.highValue || scrollView.verticalScroller.highValue < 0) ? DisplayStyle.None : DisplayStyle.Flex;

            if (hasFocus && commandField != null)
            {
                if (commandField.focusController.focusedElement != commandField)
                {
                    commandField.Focus();
                }
            }
        }
    }
}
