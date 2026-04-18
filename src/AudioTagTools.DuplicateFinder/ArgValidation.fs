module DuplicateFinder.ArgValidation

open Errors
open CCFSharpUtils
open CCFSharpUtils.Operators
open FSharpPlus
open FSharpPlus.Data
open System.IO

let validate args : Result<FileInfo * FileInfo, CommandError> =
    match args with
    | [| settingsArg; tagLibArg |] ->
        applicative {
            let! settingsFile = settingsArg |> File.toFileInfoV $"Settings file \"{settingsArg}\" does not exist."
            and! tagLibFile   = tagLibArg   |> File.toFileInfoV $"Tag library file \"{tagLibArg}\" does not exist."
            return (settingsFile, tagLibFile) }
        |> Validation.toResult
        |!! (NonEmptyList.ofList >> ArgErrors)
    | _ ->
        Error ArgCountError
