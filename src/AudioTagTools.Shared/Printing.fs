// Be careful with color because we don't know the user's terminal color scheme,
// so it's easy to unintentionally output invisible or hard-to-read text.
[<AutoOpen>]
module Printing

open System

let newLine = Environment.NewLine

let printfColor color msg =
    Console.ForegroundColor <- color
    printf $"%s{msg}"
    Console.ResetColor()

let printfnColor color msg =
    printfColor color $"{msg}{newLine}"
