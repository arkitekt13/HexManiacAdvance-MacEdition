using System;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.ViewModels;

namespace HavenSoft.HexManiac.AvaloniaUI.Windows;

public partial class MainWindow {
   // Native menu accelerators and window key bindings must share the same focus routing.
   // In particular, a disabled/read-only text operation must never fall through to ROM editing.
   private ICommand EditingCommand(Action<TextBox> textAction, Func<ICommand> fallback) => new StubCommand {
      CanExecute = _ => true,
      Execute = _ => {
         if (FocusManager?.GetFocusedElement() is TextBox box) {
            textAction(box);
         } else {
            var command = fallback();
            if (command?.CanExecute(null) == true) command.Execute(null);
         }
      },
   };

   private ICommand textCopy, textCut, textPaste, textSelectAll, textDelete, textDeepCopy;

   private void InitializeEditingCommands() {
      textCopy = EditingCommand(box => box.Copy(), () => editor?.Copy);
      textCut = EditingCommand(box => { if (!box.IsReadOnly) box.Cut(); }, () => editor?.Cut);
      textPaste = EditingCommand(box => { if (!box.IsReadOnly) box.Paste(); }, () => editor?.Paste);
      textSelectAll = EditingCommand(box => box.SelectAll(), () => editor?.SelectAll);
      textDeepCopy = EditingCommand(box => box.Copy(), () => editor?.DeepCopy);
      textDelete = EditingCommand(box => {
         if (box.IsReadOnly) return;
         if (box.SelectionStart == box.SelectionEnd && box.CaretIndex < (box.Text?.Length ?? 0))
            box.SelectionEnd = box.CaretIndex + 1;
         box.SelectedText = string.Empty;
      }, () => editor?.Delete);
      var menu = NativeMenu.GetMenu(this).Items.OfType<NativeMenuItem>().Single(item => item.Header == "Edit").Menu;
      foreach (var item in menu.Items.OfType<NativeMenuItem>()) {
         var command = item.Header switch {
            "Copy" => textCopy, "Cut" => textCut, "Paste / Replace" => textPaste,
            "Select All" => textSelectAll, "Deep Copy" => textDeepCopy, "Delete" => textDelete, _ => null,
         };
         if (command != null) item.Command = command;
      }
   }

   private void HandleTextEditingKeys(object sender, KeyEventArgs e) {
      if (FocusManager?.GetFocusedElement() is not TextBox) return;
      ICommand command = null;
      if (e.KeyModifiers == HexContent.CommandModifier) command = e.Key switch {
         Key.C => textCopy, Key.X => textCut, Key.V or Key.B => textPaste, Key.A => textSelectAll, _ => null,
      };
      if (e.KeyModifiers == (HexContent.CommandModifier | KeyModifiers.Shift) && e.Key == Key.C)
         command = textDeepCopy;
      if (command == null) return;
      e.Handled = true;
      command.Execute(null);
   }
}
