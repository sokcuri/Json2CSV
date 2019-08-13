# Json2CSV

프로그램을 실행 후 Json 파일을 끌어다 놓으면 CSV로 변환합니다

## 규칙
* CSV로 제대로 생성되기 위해서는 Json이 Array Token 형식으로 시작해야 합니다
  * [ { item1 }, { item2 } ... ]

* 필드 string, number, boolean은 내용을 그대로 씁니다
  * Field = "a" or "1" or "true"

* 필드 array-string, array-number, array-boolean은 | (파이프)로 묶습니다
  * { Field: [ "a", "b", "c", "d" ]} or { Field: [ 1, 2, 3, 4 ]}
  * Field[] = "a|b|c|d" or "1|2|3|4"

* array-object는 각각의 object를 array별로 묶습니다
  * {Field: [{ name: "a", states: 0 }, { name: "b", states: 1 }]
  * ex) Field[].name = "a|b", Field[].states = "0|1"

* 프로퍼티 값이 boolean으로 이루어진 object-object은 다음과 같이 변환합니다
  * ex) { "ParentObject": { AvailableToolkit: {"1": true, "2": true, "3": false, "4": true} } }
  * ParentObject.AvailableToolkit/True = "1,2,4"
  * ParentObject.AvailableToolkit/False = "3"



