module DuplicateFinder.ArgValidation

open Errors
open Shared.IO
open CCFSharpUtils.Library
open FSharpPlus
open FSharpPlus.Data
open System.IO

let validate args : Result<(FileInfo * FileInfo), Error> =
    match args with
    | [| settingsArg; tagLibArg |] ->
        applicative {
            let! settingsFile = settingsArg |> toFileInfo $"Settings file \"{settingsArg}\" does not exist."
            and! tagLibFile = tagLibArg |> toFileInfo $"Tag library file \"{tagLibArg}\" does not exist."
            return (settingsFile, tagLibFile)
        }
        ||! (NonEmptyList.ofList >> ArgErrors)
    | _ ->
        Error ArgCountError
