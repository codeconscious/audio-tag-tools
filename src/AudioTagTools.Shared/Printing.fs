// Be careful with color because we don't know the user's terminal color scheme,
// so it's easy to unintentionally output invisible or hard-to-read text.
[<AutoOpen>]
module Shared.Printing

open System
open Spectre.Console

type TableData = {
    Title: string option
    Headers: string list option
    Rows: string array list option
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
    // table.Border <- TableBorder.None

    match tableData.Headers with
    | Some h -> h |> List.iter (fun h' -> table.AddColumn h' |> ignore)
    | None -> ()

    match tableData.Rows with
    | Some rows -> rows |> List.iter (fun row -> table.AddRow row |> ignore)
    | None -> ()

    match tableData.Title with
    | Some title ->
        let panel = Panel(table)
        panel.Header <- PanelHeader title
        // panel.Border <- BoxBorder.None
        AnsiConsole.Write panel
    | None ->
        AnsiConsole.Write table

