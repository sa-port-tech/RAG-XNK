# Camunda 7 Run, kèm sẵn các định nghĩa quy trình của dự án.
#
#   docker build -f bpmn/Dockerfile -t xnk/camunda .
#
# ── Vì sao COPY vào image thay vì bind mount thư mục bpmn/ ────────────────────────────
# Hai lý do, cùng với các Dockerfile khác trong repo:
#
# 1. Ổ ảo Google Drive không chia sẻ được vào WSL2, và Docker mount thành thư mục RỖNG mà
#    không báo lỗi — khi đó Camunda khởi động sạch sẽ, không deploy quy trình nào, và
#    triệu chứng duy nhất là một lỗi 404 mơ hồ lúc khởi tạo instance.
# 2. Định nghĩa quy trình là **artifact triển khai**, không phải cấu hình máy. Đóng gói nó
#    vào image nghĩa là phiên bản engine và phiên bản quy trình đi cùng nhau — muốn biết
#    môi trường đang chạy bản BPMN nào thì nhìn tag image, không phải đi hỏi ai.
#
# Camunda Run tự deploy mọi file trong thư mục dưới đây lúc khởi động.

FROM camunda/camunda-bpm-platform:run-7.21.0

COPY --chown=camunda:camunda bpmn/*.bpmn /camunda/configuration/resources/
