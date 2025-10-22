module LibraryAnalysis.ArgValidation

open Errors
open System.IO

let validate (args: string array) : Result<FileInfo, Error> =
    match args with
    | [| tagLibraryPath |] -> Ok (FileInfo tagLibraryPath)
    | _ -> Error InvalidArgCount
