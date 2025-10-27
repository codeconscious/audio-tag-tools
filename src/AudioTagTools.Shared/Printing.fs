// Be careful with color because we don't know the user's terminal color scheme,
// so it's easy to unintentionally output invisible or hard-to-read text.
[<AutoOpen>]
module Shared.Printing

open System
open Spectre.Console

type TableData = {
    Title: string option
    Headers: string list option
    Rows: string array list
    ColumnAlignments: Justify list
}

let newLine = Environment.NewLine

let printfColor color msg =
    Console.ForegroundColor <- color
    printf $"%s{msg}"
    Console.ResetColor()

let printfnColor color msg =
    printfColor color $"{msg}{newLine}"

let printTable (tableData: TableData) =
    let table = Table()
    table.Border <- TableBorder.SimpleHeavy

    match tableData.Headers with
    | Some h ->
        h
        |> List.iter (fun h' -> table.AddColumn h' |> ignore)
    | None ->
        tableData.Rows
        |> List.head
        |> Array.iter (fun h' -> table.AddColumn String.Empty |> ignore)
        table.HideHeaders() |> ignore

    tableData.ColumnAlignments
    |> List.iteri (fun i a -> table.Columns[i].Alignment(a) |> ignore)

    tableData.Rows
    |> List.iter (fun row -> table.AddRow row |> ignore)


    match tableData.Title with
    | Some title ->
        let panel = Panel(table)
        panel.Header <- PanelHeader title
        // panel.Border <- BoxBorder.None
        AnsiConsole.Write panel
    | None ->
        AnsiConsole.Write table

