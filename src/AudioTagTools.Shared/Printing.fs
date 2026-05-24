[<AutoOpen>]
module Shared.Printing

open CCFSharpUtils
open CCFSharpUtils.Text
open System
open Spectre.Console
open FSharpPlus
open FSharpPlus.Data

type TableData =
    { Title: string option
      Headers: string list option
      Rows: string list nlist option
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
    match tableData.Rows with
    | None -> ()
    | Some rows ->
        let table = Table()
        table.Border <- TableBorder.SimpleHeavy

        if tableData.ShowRowSeparators then
            table.ShowRowSeparators() |> ignore

        match tableData.Headers with
        | Some header ->
            header
            |> List.iter (fun h -> h |> Markup.Escape |> table.AddColumn |> ignore)
        | None ->
            rows[0] |> List.iter (fun _ -> table.AddColumn String.Empty |> ignore)
            table.HideHeaders() |> ignore

        tableData.ColumnAlignments
        |> List.iteri (fun i alignment -> table.Columns[i].Alignment alignment |> ignore)

        rows
        |> NonEmptyList.iter (fun rowItems -> rowItems |> map Markup.Escape |> Array.ofList |> table.AddRow |> ignore)

        match tableData.Title with
        | Some tableTitle ->
            let panel = Panel table
            panel.Header <- PanelHeader $"| {Markup.Escape tableTitle} |"
            AnsiConsole.Write panel
        | None ->
            AnsiConsole.Write table
