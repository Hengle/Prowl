﻿// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Reflection;

using Prowl.Editor.Assets;
using Prowl.Editor.Preferences;
using Prowl.Icons;
using Prowl.Runtime;
using Prowl.Runtime.GUI;

namespace Prowl.Editor;

public class InspectorWindow : EditorWindow
{

    private readonly Stack<object> _BackStack = new();
    private readonly Stack<object> _ForwardStack = new();

    private WeakReference Selected;
    private bool lockSelection;

    (object, ScriptedEditor)? customEditor;

    public InspectorWindow() : base()
    {
        Title = FontAwesome6.BookOpen + " Inspector";
        GlobalSelectHandler.OnGlobalSelectObject += Selection_OnSelectObject;
    }


    private void Selection_OnSelectObject(object n)
    {
        if (lockSelection) return;

        if (n is DirectoryInfo) return; // Dont care about directories

        if (n is IAssetRef asset)
            n = asset.GetInstance();

        if (n is WeakReference weak) n = weak.Target;

        if (n == null) return;

        _ForwardStack.Clear();
        if (Selected != null)
            _BackStack.Push(Selected);
        Selected = new(n);
    }

    protected override void Close()
    {
        GlobalSelectHandler.OnGlobalSelectObject -= Selection_OnSelectObject;
    }

    protected override void Draw()
    {
        if (Selected != null && Selected.IsAlive == false)
        {
            Selected = null;
            Debug.Log("Selected object in inspector was garbage collected.");
            return;
        }

        double ItemSize = EditorStylePrefs.Instance.ItemSize;

        gui.CurrentNode.Layout(LayoutType.Column);
        gui.CurrentNode.ScaleChildren();

        using (gui.Node("Header").ExpandWidth().MaxHeight(ItemSize).Layout(LayoutType.Row).Padding(0, 10, 10, 10).Enter())
        {
            ForwardBackButtons();

            using (gui.Node("LockBtn").Scale(ItemSize).IgnoreLayout().Left(Offset.Percentage(1f, -ItemSize)).Enter())
            {
                gui.Draw2D.DrawText(lockSelection ? FontAwesome6.Lock : FontAwesome6.LockOpen, gui.CurrentNode.LayoutData.InnerRect, EditorStylePrefs.Instance.LesserText, false);

                if (gui.IsNodePressed())
                {
                    lockSelection = !lockSelection;

                    gui.Draw2D.DrawRectFilled(gui.CurrentNode.LayoutData.Rect, EditorStylePrefs.Instance.Highlighted);
                }
                else if (gui.IsNodeHovered())
                {
                    gui.Draw2D.DrawRectFilled(gui.CurrentNode.LayoutData.Rect, EditorStylePrefs.Instance.Highlighted * 0.8f);
                }
            }
        }

        using (gui.Node("Content").ExpandWidth().Padding(5, 10, 10, 10).Clip().Scroll(inputstyle: EditorGUI.InputStyle).Enter())
        {
            if (Selected == null)
            {
                gui.Draw2D.DrawRect(gui.CurrentNode.LayoutData.InnerRect, EditorStylePrefs.Instance.Warning, 2, (float)EditorStylePrefs.Instance.ButtonRoundness);
                DrawInspectorLabel("Nothing Selecting.");
                return;
            }
            if (Selected.Target is EngineObject eo1 && eo1.IsDestroyed)
            {
                gui.Draw2D.DrawRect(gui.CurrentNode.LayoutData.InnerRect, EditorStylePrefs.Instance.Warning, 2, (float)EditorStylePrefs.Instance.ButtonRoundness);
                DrawInspectorLabel("Object Destroyed.");
                return;
            }

            bool destroyCustomEditor = true;

            if (Selected.Target is FileInfo path)
            {
                if (customEditor == null)
                {
                    string? relativeAssetPath = AssetDatabase.GetRelativePath(path.FullName);
                    if (relativeAssetPath != null)
                    {
                        // The selected object is a path in our asset database, load its meta data and display a custom editor for the Importer if ones found
                        if (AssetDatabase.TryGetGuid(path, out Guid id))
                        {
                            var meta = MetaFile.Load(path);
                            if (meta != null)
                            {
                                ScriptedEditor? editor = ScriptedEditor.CreateEditor(meta, meta.importer.GetType(), false);
                                if (editor != null)
                                {
                                    customEditor = (path, editor);
                                    destroyCustomEditor = false;
                                }
                                else
                                {
                                    // Dummy Node
                                    DrawInspectorLabel("No Editor Found: " + path.FullName);
                                }
                            }
                            else
                            {
                                DrawInspectorLabel("No Meta File: " + path.FullName);
                            }
                        }
                        else
                        {
                            DrawInspectorLabel("File in Assets folder: " + path.FullName);
                        }
                    }
                    else
                    {
                        DrawInspectorLabel("FileInfo: " + path.FullName);
                    }
                }
                else if (customEditor.Value.Item1.Equals(path))
                {
                    // We are still editing the same asset path
                    customEditor.Value.Item2.OnInspectorGUI(new());
                    destroyCustomEditor = false;
                }
            }
            else
            {
                if (customEditor == null)
                {
                    ScriptedEditor? editor = ScriptedEditor.CreateEditor(Selected.Target);
                    if (editor != null)
                    {
                        customEditor = (Selected.Target, editor);
                        destroyCustomEditor = false;
                    }
                }
                else if (customEditor.Value.Item1 == Selected.Target)
                {
                    // We are still editing the same object
                    customEditor.Value.Item2.OnInspectorGUI(new());
                    destroyCustomEditor = false;
                }
            }

            if (destroyCustomEditor)
            {
                customEditor?.Item2.OnDisable();
                customEditor = null;
            }
        }
    }

    private void DrawInspectorLabel(string message)
    {
        double ItemSize = EditorStylePrefs.Instance.ItemSize;

        gui.Node("DummyForText").ExpandWidth().Height(ItemSize * 10);
        gui.Draw2D.DrawText(message, gui.CurrentNode.LayoutData.Rect);
    }

    private void ForwardBackButtons()
    {
        double ItemSize = EditorStylePrefs.Instance.ItemSize;

        // remove nulls or destroyed
        while (_BackStack.Count > 0)
        {
            var peek = _BackStack.Peek();
            if (peek == null || (peek is EngineObject eo2 && eo2.IsDestroyed) || ReferenceEquals(peek, Selected?.Target))
                _BackStack.Pop();
            else
                break;
        }

        using (gui.Node("BackBtn").Scale(ItemSize).Enter())
        {
            Color backCol = _BackStack.Count == 0 ? Color.white * 0.7f : Color.white;
            gui.Draw2D.DrawText(FontAwesome6.ArrowLeft, gui.CurrentNode.LayoutData.InnerRect, backCol, false);
            if (_BackStack.Count != 0)
            {
                if (gui.IsNodePressed())
                {
                    _ForwardStack.Push(Selected.Target);
                    Selected = new(_BackStack.Pop());

                    gui.Draw2D.DrawRectFilled(gui.CurrentNode.LayoutData.Rect, EditorStylePrefs.Instance.Highlighted);
                }
                else if (gui.IsNodeHovered())
                {
                    gui.Draw2D.DrawRectFilled(gui.CurrentNode.LayoutData.Rect, EditorStylePrefs.Instance.Highlighted * 0.8f);
                }
            }
        }


        // remove nulls or destroyed
        while (_ForwardStack.Count > 0)
        {
            var peek = _ForwardStack.Peek();
            if (peek == null || (peek is EngineObject eo3 && eo3.IsDestroyed) || ReferenceEquals(peek, Selected.Target))
                _ForwardStack.Pop();
            else
                break;
        }

        using (gui.Node("ForwardBtn").Scale(ItemSize).Enter())
        {
            Color forwardCol = _ForwardStack.Count == 0 ? Color.white * 0.7f : Color.white;
            gui.Draw2D.DrawText(FontAwesome6.ArrowRight, gui.CurrentNode.LayoutData.InnerRect, forwardCol, false);

            if (gui.IsNodePressed())
            {
                if (gui.IsNodePressed())
                {
                    _BackStack.Push(Selected.Target);
                    Selected = new(_ForwardStack.Pop());

                    gui.Draw2D.DrawRectFilled(gui.CurrentNode.LayoutData.Rect, EditorStylePrefs.Instance.Highlighted);
                }
                else if (gui.IsNodeHovered())
                {
                    gui.Draw2D.DrawRectFilled(gui.CurrentNode.LayoutData.Rect, EditorStylePrefs.Instance.Highlighted * 0.8f);
                }
            }
        }
    }

}
