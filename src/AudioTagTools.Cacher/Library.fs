module AudioTagTools.Cacher

open System
open Errors
open IO
open Tags
open ArgValidation
open FsToolkit.ErrorHandling
open AudioTagTools.Shared.IO

let private run (args: string array) : Result<unit, Error> =
    result {
        let! mediaDir, tagLibraryFile = validate args
        let! fileInfos = getFileInfos mediaDir
        let! tagLibraryMap = createTagLibraryMap tagLibraryFile
        let! newJson = generateNewJson tagLibraryMap fileInfos

        let _ =
            tagLibraryFile
            |> copyToBackupFile
            |> Result.tee (fun backupFile -> printfn "Backed up previous file to \"%s\"." backupFile.Name)
            |> Result.mapError WriteFileError

        do!
            newJson
            |> writeTextToFile tagLibraryFile.FullName
            |> Result.tee (fun _ -> printfn "Wrote file \"%s\"." tagLibraryFile.FullName)
            |> Result.mapError WriteFileError
    }

let start (args: string array) : Result<string, string> =
    match run args with
    | Ok _ -> Ok "Finished caching successfully!"
    | Error e -> Error (message e)
