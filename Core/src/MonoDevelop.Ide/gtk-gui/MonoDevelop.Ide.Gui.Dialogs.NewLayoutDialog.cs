// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      Mono Runtime Version: 2.0.50727.42
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------

namespace MonoDevelop.Ide.Gui.Dialogs {
    
    
    internal partial class NewLayoutDialog {
        
        private Gtk.HBox hbox45;
        
        private Gtk.Label label72;
        
        private Gtk.Entry layoutName;
        
        private Gtk.Button button309;
        
        private Gtk.Button newButton;
        
        protected virtual void Build() {
            Stetic.Gui.Initialize();
            // Widget MonoDevelop.Ide.Gui.Dialogs.NewLayoutDialog
            this.Name = "MonoDevelop.Ide.Gui.Dialogs.NewLayoutDialog";
            this.Title = "New Layout";
            this.TypeHint = ((Gdk.WindowTypeHint)(1));
            this.BorderWidth = ((uint)(6));
            this.Resizable = false;
            this.AllowGrow = false;
            this.HasSeparator = false;
            // Internal child MonoDevelop.Ide.Gui.Dialogs.NewLayoutDialog.VBox
            Gtk.VBox w1 = this.VBox;
            w1.Name = "dialog-vbox4";
            w1.Spacing = 6;
            // Container child dialog-vbox4.Gtk.Box+BoxChild
            this.hbox45 = new Gtk.HBox();
            this.hbox45.Name = "hbox45";
            this.hbox45.Spacing = 6;
            this.hbox45.BorderWidth = ((uint)(6));
            // Container child hbox45.Gtk.Box+BoxChild
            this.label72 = new Gtk.Label();
            this.label72.Name = "label72";
            this.label72.Xalign = 0F;
            this.label72.LabelProp = Mono.Unix.Catalog.GetString("Layout name:");
            this.hbox45.Add(this.label72);
            Gtk.Box.BoxChild w2 = ((Gtk.Box.BoxChild)(this.hbox45[this.label72]));
            w2.Position = 0;
            w2.Expand = false;
            w2.Fill = false;
            // Container child hbox45.Gtk.Box+BoxChild
            this.layoutName = new Gtk.Entry();
            this.layoutName.Name = "layoutName";
            this.layoutName.IsEditable = true;
            this.layoutName.ActivatesDefault = true;
            this.layoutName.InvisibleChar = '●';
            this.hbox45.Add(this.layoutName);
            Gtk.Box.BoxChild w3 = ((Gtk.Box.BoxChild)(this.hbox45[this.layoutName]));
            w3.Position = 1;
            w1.Add(this.hbox45);
            Gtk.Box.BoxChild w4 = ((Gtk.Box.BoxChild)(w1[this.hbox45]));
            w4.Position = 0;
            // Internal child MonoDevelop.Ide.Gui.Dialogs.NewLayoutDialog.ActionArea
            Gtk.HButtonBox w5 = this.ActionArea;
            w5.Name = "GtkDialog_ActionArea";
            w5.Spacing = 10;
            w5.BorderWidth = ((uint)(5));
            w5.LayoutStyle = ((Gtk.ButtonBoxStyle)(4));
            // Container child GtkDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
            this.button309 = new Gtk.Button();
            this.button309.CanFocus = true;
            this.button309.Name = "button309";
            this.button309.UseStock = true;
            this.button309.UseUnderline = true;
            this.button309.Label = "gtk-cancel";
            this.AddActionWidget(this.button309, -6);
            Gtk.ButtonBox.ButtonBoxChild w6 = ((Gtk.ButtonBox.ButtonBoxChild)(w5[this.button309]));
            w6.Expand = false;
            w6.Fill = false;
            // Container child GtkDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
            this.newButton = new Gtk.Button();
            this.newButton.CanFocus = true;
            this.newButton.Name = "newButton";
            this.newButton.UseStock = true;
            this.newButton.UseUnderline = true;
            this.newButton.Label = "gtk-new";
            this.AddActionWidget(this.newButton, -5);
            Gtk.ButtonBox.ButtonBoxChild w7 = ((Gtk.ButtonBox.ButtonBoxChild)(w5[this.newButton]));
            w7.Position = 1;
            w7.Expand = false;
            w7.Fill = false;
            if ((this.Child != null)) {
                this.Child.ShowAll();
            }
            this.DefaultWidth = 363;
            this.DefaultHeight = 131;
            this.Show();
        }
    }
}
