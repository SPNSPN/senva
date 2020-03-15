SRC := src
BLD := build

.PHONY: all
all: $(BLD)/repl.jar $(BLD)/test.jar

$(BLD)/repl.jar: $(SRC)/repl.kt $(SRC)/interpreter.kt
	kotlinc-jvm $^ -module-name Repl -include-runtime -d $(BLD)/repl.jar

$(BLD)/test.jar: $(SRC)/test.kt $(SRC)/interpreter.kt
	kotlinc-jvm $^ -module-name Test -include-runtime -d $(BLD)/test.jar

.PHONY: repl test clean
repl: $(BLD)/repl.jar
	rlwrap java -jar $^ $(ARGS)

test: $(BLD)/test.jar
	java -jar $^

clean:
	rm $(BLD)/*

