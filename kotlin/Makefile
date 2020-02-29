.PHONY: all
all: repl.jar test.jar

repl.jar: repl.kt interpreter/interpreter.kt
	kotlinc-jvm $^ -module-name Repl -include-runtime -d repl.jar

test.jar: test.kt interpreter/interpreter.kt
	kotlinc-jvm $^ -module-name Test -include-runtime -d test.jar

.PHONY: repl test clean
repl: repl.jar
	java -jar repl.jar $(ARGS)

test: test.jar
	java -jar test.jar

clean:
	rm repl.jar test.jar

