module LibraryAnalysis.Errors

type Error =
    | ArgInvalidCount
    | IoMissingFile of string
    | IoReadError of string
    | TagParseError of string

let message = function
    | ArgInvalidCount -> "Invalid arguments. Pass only your tag library path."
    | IoMissingFile file -> $"File \"{file}\" was not found."
    | IoReadError msg -> $"I/O read failure: {msg}"
    | TagParseError msg -> $"Unable to parse the tag library file: {msg}"
