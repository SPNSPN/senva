CSC := '/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe'
LIB := 'C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319,C:\\Windows\Microsoft.NET\\assembly\\GAC_MSIL'

SRC := src
BLD := build

.PHONY: all
all: $(BLD)/repl.exe $(BLD)/test.exe

$(BLD)/repl.exe: $(SRC)/Repl.cs $(SRC)/Senva.cs
	$(CSC) /out:'$(BLD)\\repl.exe' /lib:$(LIB) $(SRC)\\Repl.cs $(SRC)\\Senva.cs

$(BLD)/test.exe: $(SRC)/Test.cs $(SRC)/Senva.cs
	$(CSC) /out:'$(BLD)\\test.exe' /lib:$(LIB) $(SRC)\\Test.cs $(SRC)\\Senva.cs

.PHONY: repl test clean
repl: $(BLD)/repl.exe
	$(BLD)/repl.exe

test: $(BLD)/test.exe
	$(BLD)/test.exe

clean:
	rm $(BLD)/*
