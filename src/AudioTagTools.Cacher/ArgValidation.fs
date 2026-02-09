module Cacher.ArgValidation

open Errors
open Shared.IO
open FSharpPlus
open FSharpPlus.Data
open System.IO

let validate args : Result<(DirectoryInfo * FileInfo), Error> =
    match args with
    | [| mediaDirArg; tagLibArg |] ->
        (fun mediaDir -> (mediaDir, FileInfo tagLibArg))
        <!> (mediaDirArg |> validateToDirInfo (MediaDirectoryMissing mediaDirArg))
        |> Validation.toResult
    | _ ->
        Error InvalidArgCount

