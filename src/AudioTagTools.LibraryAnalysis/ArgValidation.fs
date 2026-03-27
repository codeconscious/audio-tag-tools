module LibraryAnalysis.ArgValidation

open Errors
open System.IO

let validate (args: string array) : Result<FileInfo, Error> =
    match args with
    | [| tagLibraryArg |] ->
        if File.Exists tagLibraryArg
        then Ok (FileInfo tagLibraryArg)
        else Error (FileMissing tagLibraryArg)
    | _ ->
        Error ArgInvalidCount
