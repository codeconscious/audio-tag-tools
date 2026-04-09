[<AutoOpen>]
module Shared.Printing

open CCFSharpUtils
open System
open Spectre.Console

type TableData =
    { Title: string option
      Headers: string list option
      Rows: string list list
      ColumnAlignments: Justify list
      ShowRowSeparators: bool }

// Be careful with color because we don't know the user's terminal color scheme,
// so it's easy to unintentionally output invisible or hard-to-read text.
let printfColor color msg =
    Console.ForegroundColor <- color
    printf $"%s{msg}"
    Console.ResetColor()

let printfnColor color msg =
    printfColor color $"{msg}{String.nl}"

let printTable tableData =
    let table = Table()
    table.Border <- TableBorder.SimpleHeavy

    if tableData.ShowRowSeparators then
        table.ShowRowSeparators() |> ignore

    match tableData.Headers with
    | Some header ->
        header
        |> List.iter (fun h -> h |> Markup.Escape |> table.AddColumn |> ignore)
    | None ->
        tableData.Rows[0] |> List.iter (fun _ -> table.AddColumn String.Empty |> ignore)
        table.HideHeaders() |> ignore

    tableData.ColumnAlignments
    |> List.iteri (fun i alignment -> table.Columns[i].Alignment alignment |> ignore)

    tableData.Rows
    |> List.iter (fun rowItems -> rowItems |> List.map Markup.Escape |> Array.ofList |> table.AddRow |> ignore)

    match tableData.Title with
    | Some tableTitle ->
        let panel = Panel table
        panel.Header <- PanelHeader $"| {Markup.Escape tableTitle} |"
        AnsiConsole.Write panel
    | None ->
        AnsiConsole.Write table
