// Be careful with color because we don't know the user's terminal color scheme,
// so it's easy to unintentionally output invisible or hard-to-read text.
[<AutoOpen>]
module Shared.Printing

open System
open Spectre.Console

type TableData =
    { Title: string option
      Headers: string list option
      Rows: string array array
      ColumnAlignments: Justify list }

let newLine = Environment.NewLine

let printfColor color msg =
    Console.ForegroundColor <- color
    printf $"%s{msg}"
    Console.ResetColor()

let printfnColor color msg =
    printfColor color $"{msg}{newLine}"

let printTable tableData =
    let table = Table()
    table.Border <- TableBorder.SimpleHeavy

    match tableData.Headers with
    | Some header ->
        header |> List.iter (fun h -> table.AddColumn h |> ignore)
    | None ->
        tableData.Rows[0] |> Array.iter (fun _ -> table.AddColumn String.Empty |> ignore)
        table.HideHeaders() |> ignore

    tableData.ColumnAlignments
    |> List.iteri (fun i alignment -> table.Columns[i].Alignment(alignment) |> ignore)

    tableData.Rows
    |> Array.iter (fun row -> table.AddRow row |> ignore)

    match tableData.Title with
    | Some tableTitle ->
        let panel = Panel table
        panel.Header <- PanelHeader $"| {tableTitle} |"
        AnsiConsole.Write panel
    | None ->
        AnsiConsole.Write table

