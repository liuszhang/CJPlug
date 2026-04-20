@echo off
setlocal enabledelayedexpansion
set "command=C:\MyPro\MyElsa\ToolMng\toolAgentFiles\bats\a b\ToolExecuteTest.exe"
set "params=""a"" ""b"" ""c"" ""--a"" ""1111"" ""--b"" ""2222"" ""--c"" ""3333"""

rem 使用enabledelayedexpansion来处理包含空格的变量
set "fullCommand=start "" "!command!" "a" "b" "c" "--a" "1111" "--b" "2222" "--c" "3333""
echo !fullCommand!
call !fullCommand!
endlocal