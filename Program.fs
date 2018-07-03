open System
open System.Windows.Forms
open System.Drawing
open K3

[<EntryPoint>]
let main argv = 
  let calc = History RpnCalc.empty

  let execute text =
    if   text = "ac" then calc.Clear(); true
    elif text = "undo" then calc.Undo(); true
    elif text = "redo" then calc.Redo(); true
    else
      match calc.Current |> RpnCalc.execute text with
      | Some x -> calc.Commit x; true
      | None -> false

  let form = new Form(ClientSize = Size(400, 200), Text = "K3 - RPN 電卓")
  let split = new SplitContainer (Dock = DockStyle.Fill)
  let stackView   = new ListBox ()
  let varView     = new ListBox ()
  let commandView = new ListBox ()
  let inputBox    = new TextBox ()
  let btnClear    = new Button (Text = "c",  Tag = "c")
  let btnAllClear = new Button (Text = "ac", Tag = "ac")
  let btnUndo     = new Button (Text = "<<", Tag = "undo")
  let btnRedo     = new Button (Text = ">>", Tag = "redo")

  calc.Changed.Add (fun _ ->
    stackView.Items.Clear()
    calc.Current.Stack
    |> List.rev |> List.map (fun x ->
      if 1.0e-4 < abs x && abs x < 1.0e10
        then sprintf "%22.10f" x
        else sprintf "%e" x)
    |> List.iter (stackView.Items.Add >> ignore)

    varView.Items.Clear()
    calc.Current.Vars |> Map.toSeq |> Seq.iter (fun (name, value) ->
      sprintf ".%s = %f" name value |> varView.Items.Add |> ignore))

  let executeByInputBox () =
    if execute inputBox.Text
      then inputBox.Text <- ""
      else inputBox.SelectAll()

  let searchCommands () =
    let cmds =
      seq {
        yield "ac"
        if calc.CanUndo then yield "undo"
        if calc.CanRedo then yield "redo"
      }
    let ops =
      RpnCalc.operators |> Map.toSeq
      |> Seq.filter (snd >> fun c -> c.CanExecute calc.Current)
      |> Seq.map fst
    let vars =
      calc.Current.Vars
      |> Map.toSeq
      |> Seq.map (fst >> sprintf ".%s")
    commandView.Items.Clear()
    Seq.concat [cmds; ops; vars]
    |> Seq.filter (fun s -> s.StartsWith inputBox.Text)
    |> Seq.iter (commandView.Items.Add >> ignore)

  searchCommands ()

  inputBox.KeyUp.Add (fun e ->
    match e.KeyCode with
    | Keys.Enter -> executeByInputBox ()
    | Keys.Tab when commandView.Items.Count > 0 ->
      commandView.SelectedIndex <- (commandView.SelectedIndex + 1) % commandView.Items.Count
    | _ -> ())
      
  let mutable isCommandSearchEnabled = true

  inputBox.TextChanged
  |> Event.filter (fun _ -> isCommandSearchEnabled)
  |> Event.add (fun _ -> searchCommands ())

  [btnClear; btnAllClear; btnUndo; btnRedo] |> List.iter (fun b ->
    b.Click.Add (fun _ -> execute (b.Tag :?> string) |> ignore))

  commandView.SelectedIndexChanged.Add (fun _ ->
    match commandView.SelectedItem with
    | :? string as s ->
      isCommandSearchEnabled <- false
      inputBox.Text <- s
      inputBox.Focus() |> ignore
      inputBox.SelectAll()
      isCommandSearchEnabled <- true
    | _ -> ())

  varView.SelectedIndexChanged.Add (fun _ ->
    if varView.SelectedIndex <> -1 then
      let name = (calc.Current.Vars |> Map.toArray).[varView.SelectedIndex] |> fst
      inputBox.Text <- "." + name
      inputBox.Focus() |> ignore
      inputBox.SelectAll())

  commandView.DoubleClick.Add (fun _ -> executeByInputBox())
  varView    .DoubleClick.Add (fun _ -> executeByInputBox())

  form.ResumeLayout()

  form.Icon <- Icon.ExtractAssociatedIcon Application.ExecutablePath
  form.TopMost <- true
  form.Location <-
    let desktop = (Screen.FromControl form).WorkingArea
    let x = desktop.Right  - form.Width - 24
    let y = desktop.Bottom - form.Height- 24
    Point( x, y )

  form.Controls.Add split
  split.SplitterDistance <- 250
  split.SplitterWidth <- 4
  split.TabStop <- false

  let anchorAll = AnchorStyles.Top ||| AnchorStyles.Bottom ||| AnchorStyles.Left ||| AnchorStyles.Right

  split.Panel1.Controls.Add stackView
  stackView.Height <- 120
  stackView.Width <- split.Panel1.Width
  stackView.Anchor <- anchorAll
  stackView.BackColor <- SystemColors.Control
  stackView.BorderStyle <- BorderStyle.None
  stackView.Font <- new Font("Consolas", 14.f)
  stackView.TabStop <- false

  split.Panel1.Controls.Add inputBox
  inputBox.Top <- stackView.Bottom
  inputBox.Width <-stackView.Width
  inputBox.Anchor <- AnchorStyles.Bottom ||| AnchorStyles.Left ||| AnchorStyles.Right
  inputBox.TextAlign <- HorizontalAlignment.Right
  inputBox.Font <- new Font("Consolas", 16.f)
  inputBox.BorderStyle <- BorderStyle.FixedSingle

  [btnClear; btnAllClear; btnUndo; btnRedo] |> List.iter (fun b ->
    split.Panel1.Controls.Add b
    b.TabStop <- false
    b.FlatStyle <- FlatStyle.Flat
    b.Top <- inputBox.Bottom + 4
    b.Size <- Size (40, 20))
  [btnClear; btnUndo; btnRedo] |> List.iter (fun b ->
    b.Anchor <- AnchorStyles.Right ||| AnchorStyles.Bottom)
  btnAllClear.Anchor <- AnchorStyles.Left ||| AnchorStyles.Bottom
  btnAllClear.Left <- inputBox.Left
  btnClear.Left <- inputBox.Right - 40
  btnRedo.Left <- btnClear.Left - 40 - 4
  btnUndo.Left <- btnRedo.Left - 40 - 4

  split.Panel2.Controls.Add varView
  varView.Height <- stackView.Height
  varView.Width <- split.Panel2.Width
  varView.Anchor <- anchorAll
  varView.BackColor <- SystemColors.Control
  varView.BorderStyle <- BorderStyle.None
  varView.Font <- new Font("Consolas", 9.f)
  varView.TabStop <- false

  split.Panel2.Controls.Add commandView
  commandView.Top <- stackView.Bottom
  commandView.Width <- split.Panel2.Width
  commandView.Height <- split.Panel2.Height - varView.Height
  commandView.Anchor <- AnchorStyles.Bottom ||| AnchorStyles.Left ||| AnchorStyles.Right
  commandView.BackColor <- SystemColors.Control
  commandView.BorderStyle <- BorderStyle.FixedSingle
  commandView.Font <- new Font("Consolas", 9.f)
  commandView.TabStop <- false

  form.PerformLayout()


  Application.Run form
  0
