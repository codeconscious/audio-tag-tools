module Cacher.ArgValidation

open Errors
open CCFSharpUtils.Library
open FSharpPlus
open FSharpPlus.Data
open System.IO

let validate args =
    match args with
    | [| mediaDirArg; tagLibArg |] ->
        (fun mediaDir -> (DirectoryInfo mediaDir, FileInfo tagLibArg))
        <!> (mediaDirArg |> Directory.verifyExists (MediaDirectoryMissing mediaDirArg))
    | _ ->
        Failure InvalidArgCount
    |> Validation.toResult
