module Cacher.ArgValidation

open Errors
open CCFSharpUtils.Library
open FSharpPlus
open FSharpPlus.Data
open System.IO

let validate args : Result<(DirectoryInfo * FileInfo), Error> =
    match args with
    | [| mediaDirArg; tagLibArg |] ->
        (fun mediaDir -> (DirectoryInfo mediaDir, FileInfo tagLibArg))
        <!> (mediaDirArg |> Directory.verifyExists (MediaDirectoryMissing mediaDirArg))
        |> Validation.toResult
    | _ ->
        Error InvalidArgCount
