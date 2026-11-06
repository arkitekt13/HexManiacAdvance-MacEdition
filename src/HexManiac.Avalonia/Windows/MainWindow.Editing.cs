using System;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;

namespace HavenSoft.HexManiac.AvaloniaUI.Windows;

public partial class MainWindow {
   private bool ScriptReferenceActive => editor?.PreferScriptMapView == true && editor.SelectedTab is ViewPort view &&
      view.Tools?.SelectedTool is CodeTool { Mode: CodeMode.Script };

   private TextBox FocusReferenceScriptInput() {
      return Tabs.GetVisualDescendants().OfType<TabView>()
         .FirstOrDefault(view => ReferenceEquals(view.DataContext, editor?.SelectedTab) && view.IsEffectivelyVisible)?.FocusScriptInput();
   }
   // Native menu accelerators and window key bindings must share the same focus routing.
   // In particular, a disabled/read-only text operation must never fall through to ROM editing.
   private PaletteControl FocusedPalette => FocusManager?.GetFocusedElement() is Visual focused
      ? focused as PaletteControl ?? focused.GetVisualAncestors().OfType<PaletteControl>().FirstOrDefault() : null;

   private ICommand EditingCommand(Action<TextBox> textAction, Func<ICommand> fallback,
      Action<PaletteControl> paletteAction = null) => new StubCommand {
      CanExecute = _ => true,
      Execute = _ => {
         if (FocusManager?.GetFocusedElement() is TextBox box && (!ScriptReferenceActive || box.IsEffectivelyVisible)) {
            textAction(box);
         } else if (ScriptReferenceActive) {
            // A reference pane must never route editing into the hidden hex selection.
            var script = FocusReferenceScriptInput();
            if (script != null) textAction(script);
         } else if (FocusedPalette is PaletteControl palette) {
            // Unsupported operations in a palette must not fall through to ROM/image edits.
            paletteAction?.Invoke(palette);
         } else {
            var command = fallback();
            if (command?.CanExecute(null) == true) command.Execute(null);
         }
      },
   };

   private ICommand textCopy, textCut, textPaste, textSelectAll, textDelete, textDeepCopy;

   private void InitializeEditingCommands() {
      textCopy = EditingCommand(box => box.Copy(), () => editor?.Copy, p => p.EditClipboard(false));
      textCut = EditingCommand(box => { if (!box.IsReadOnly) box.Cut(); }, () => editor?.Cut);
      textPaste = EditingCommand(box => { if (!box.IsReadOnly) box.Paste(); }, () => editor?.Paste, p => p.EditClipboard(true));
      textSelectAll = EditingCommand(box => box.SelectAll(), () => editor?.SelectAll, p => p.SelectAllColors());
      textDeepCopy = EditingCommand(box => box.Copy(), () => editor?.DeepCopy, p => p.EditClipboard(false));
      textDelete = EditingCommand(box => {
         if (box.IsReadOnly) return;
         if (box.SelectionStart == box.SelectionEnd && box.CaretIndex < (box.Text?.Length ?? 0))
            box.SelectionEnd = box.CaretIndex + 1;
         box.SelectedText = string.Empty;
      }, () => editor?.Delete, p => p.RunPaletteCommand(v => v.DeleteColor));
      var menu = NativeMenu.GetMenu(this).Items.OfType<NativeMenuItem>().Single(item => item.Header == "Edit").Menu;
      foreach (var item in menu.Items.OfType<NativeMenuItem>()) {
         var command = item.Header switch {
            "Copy" => textCopy, "Cut" => textCut, "Paste / Replace" => textPaste,
            "Select All" => textSelectAll, "Deep Copy" => textDeepCopy, "Delete" => textDelete, _ => null,
         };
         if (command != null) item.Command = command;
      }
   }

   private void CancelReferencePanOnEscape(object sender, KeyEventArgs e) {
      if (ScriptReferenceActive && e.Key == Key.Escape)
         Tabs.GetVisualDescendants().OfType<TabView>()
            .FirstOrDefault(view => ReferenceEquals(view.DataContext, editor?.SelectedTab) && view.IsEffectivelyVisible)?.CancelReferencePan();
   }

   private void HandleTextEditingKeys(object sender, KeyEventArgs e) {
      var palette = FocusedPalette;
      bool textFocus = FocusManager?.GetFocusedElement() is TextBox;
      if (!textFocus && palette == null && !ScriptReferenceActive) return;
      if (!textFocus && palette != null && e.KeyModifiers == HexContent.CommandModifier && (e.Key == Key.G || e.Key == Key.M)) {
         e.Handled = true;
         palette.RunPaletteCommand(p => e.Key == Key.G ? p.CreateGradient : p.SingleReduce);
         return;
      }
      ICommand command = null;
      if (e.KeyModifiers == HexContent.CommandModifier) command = e.Key switch {
         Key.C => textCopy, Key.X => textCut, Key.V or Key.B => textPaste, Key.A => textSelectAll, _ => null,
      };
      if (e.KeyModifiers == (HexContent.CommandModifier | KeyModifiers.Shift) && e.Key == Key.C)
         command = textDeepCopy;
      if (!textFocus && e.KeyModifiers == KeyModifiers.None && e.Key == Key.Delete) command = textDelete;
      if (command == null) return;
      e.Handled = true;
      command.Execute(null);
   }
}
