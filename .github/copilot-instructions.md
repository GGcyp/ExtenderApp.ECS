# Copilot Instructions

## Project Guidelines
- 用户偏好代码注释使用中文。
- 在 ECS 查询设计上，优先考虑减少重复泛型类型：倾向于保留一个核心泛型查询类型，其余组合层尽量用结构体或按需拼接，而不是为每个泛型数量复制整套实现。