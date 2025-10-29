module LibraryAnalysis.Errors

type Error =
    | InvalidArgCount
    | IoReadError of string
    | TagParseError of string

let message = function
    | InvalidArgCount -> "Invalid arguments. Pass only your tag library path."
    | IoReadError msg -> $"I/O read failure: {msg}"
    | TagParseError msg -> $"Unable to parse the tag library file: {msg}"
