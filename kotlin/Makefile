SRC := $(wildcard *.kt)
LPRJ := $(patsubst %.kt,%,$(SRC))
PRJ = $(shell echo $(LPRJ) | sed 's/\(.\)\(.*\)/\U\1\L\2/g')
LIB := $(shell echo ${KOTLIN_LIB}/kotlin-runtime.jar:.)

$(LPRJ).jar: $(SRC)
	kotlinc-jvm $^ -module-name $(PRJ) -include-runtime -d $(LPRJ).jar

.PHONY: run
run: $(LPRJ).jar
	java -jar $(LPRJ).jar $(ARGS)

