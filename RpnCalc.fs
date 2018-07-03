namespace K3
open System

type RpnCalc = {
    Stack : float list
    Vars : Map<string, float>
  }

type IOperator =
  abstract CanExecute : RpnCalc -> bool
  abstract Execute : RpnCalc -> RpnCalc option


[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module RpnCalc =

  let empty = { Stack = []; Vars = Map.empty }

  let private makeOp f g =
    { new IOperator with
        member __.CanExecute self = f self.Stack
        member __.Execute self = g self.Stack |> Option.map (fun s -> { self with Stack = s })
    }

  let private makeOp' = makeOp (List.isEmpty >> not)

  let private unaryOp f =
    makeOp' (function x :: stack -> Some (f x :: stack) | _ -> None)

  let private binaryOp f =
    makeOp
      (function _ :: _ :: _ -> true | _ -> false)
      (function x :: y :: stack -> Some (f y x :: stack) | _ -> None)

  let private constant value =
    makeOp (fun _ -> true) (fun stack -> Some (value :: stack))

  let operators =
    [ "+", binaryOp (+)
      "-", binaryOp (-)
      "*", binaryOp (*)
      "/", binaryOp (/)
      "^",  binaryOp (fun a b -> Math.Pow (a, b))
      "**", binaryOp (fun a b -> Math.Pow (a, b))
      "sin",  unaryOp sin
      "cos",  unaryOp cos
      "tan",  unaryOp tan
      "asin", unaryOp asin
      "acos", unaryOp acos
      "atan", unaryOp atan
      "sqrt", unaryOp sqrt
      "log",  unaryOp log
      "log10", unaryOp log10
      "abs",  unaryOp abs
      "ceil", unaryOp ceil
      "floor", unaryOp floor
      "round", unaryOp round
      "exp",  unaryOp exp
      "pi", constant Math.PI
      "e",  constant Math.E
      "sum", makeOp' (fun stack -> Some [stack |> List.sum])
      "c", makeOp' (fun _ -> Some [])
      "d", makeOp' (function _ :: tail -> Some tail | _ -> None)
    ] |> Map.ofList

  let execute text self =
    let ok, value = Double.TryParse text
    if ok then Some { self with Stack = value :: self.Stack } else

    if text.StartsWith "var " && not self.Stack.IsEmpty then
      let tokens = text.Split ([|' '; '\t'|], StringSplitOptions.RemoveEmptyEntries)
      if tokens.Length = 2
        then Some { self with Vars = self.Vars |> Map.add tokens.[1] self.Stack.Head }
        else None
    elif text.StartsWith "." then
      self.Vars |> Map.tryFind (text.TrimStart '.')
      |> Option.map (fun x -> { self with Stack = x :: self.Stack })
    else
      operators |> Map.tryFind text
      |> Option.filter (fun op -> op.CanExecute self)
      |> Option.bind (fun op -> op.Execute self)


