namespace K3
open System
open System.Collections.Generic

type History<'a> (init) =
  let mutable current = init
  let undoBuffer = Stack<'a>()
  let redoBuffer = Stack<'a>()
  let changed = Event<_>()

  member __.Current = current
  member __.Changed = changed.Publish

  member __.CanUndo = undoBuffer.Count > 0
  member __.CanRedo = redoBuffer.Count > 0

  member __.Commit a =
    undoBuffer.Push current
    redoBuffer.Clear ()
    current <- a
    changed.Trigger EventArgs.Empty

  member __.Undo () =
    if undoBuffer.Count > 0 then
      redoBuffer.Push current
      current <- undoBuffer.Pop()
      changed.Trigger EventArgs.Empty

  member __.Redo () =
    if redoBuffer.Count > 0 then
      undoBuffer.Push current
      current <- redoBuffer.Pop()
      changed.Trigger EventArgs.Empty

  member __.Clear () =
    current <- init
    undoBuffer.Clear ()
    redoBuffer.Clear ()
    changed.Trigger EventArgs.Empty
    

