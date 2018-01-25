# SharpPeg
A C# implementation of Parsing Expression Grammars, intended to be used as a replacement for regular expressions. SharpPeg will dynamically compile PEGs to CIL, delivering performance similar to that of common regular expression engines.

# `peg-match`
The `peg-match` tool uses SharpPeg to implement a tool similar to `grep`, using PEGs instead of regular expressions.

## Installation
The latest stable release of `peg-match` can be downloaded from [https://github.com/Jos635/SharpPeg/releases](the releases). Extract the folder corresponding with your platform of choice to a directory, for example, for Windows you should extract the `win\x64` folder. From the terminal, `cd` to this folder and run `./peg-match`.

Note: on linux, you might still need to install some dependencies. For example, Ubuntu requires libunwind to be installed:
```
sudo apt install libunwind8
```

## How to use
`peg-match` can be used by invoking it from the command line. For example:

```
./peg-match '"th" [a-z]+' input.txt
```

The command above will print all occurrences of words starting with 'th' in the file `input.txt`.

A part of the expression may be printed by using captures:

```
./peg-match -c word '"the " {word: [a-z]+}' input.txt
```

The command above will match all words prefixed by "the", but it will not print "the" in the output.

`peg-match` can optionally output structured data instead of plain text by using the `-j` switch:

```
./peg-match -j File::Csv::CsvFile data.csv
```

The command above will parse the csv file, and output the parsed data as a CSV file. Any grammar containing one or more captures can be converted to JSON. For example, a very simple expression to match `key=value` pairs might look like this:

```
./peg-match -j '{key: [A-Za-z0-9]+}"="{value: [A-Za-z0-9]+}' data.txt
```

Which will output a JSON object that looks something like this:
```
[
  {
    "key": "username",
    "value": "jos"
  },
  {
    "key": "password",
    "value": "hunter1"
  }
]
```

# License
This work is licensed under the LGPL license. Please see the LICENSE file for more info.
