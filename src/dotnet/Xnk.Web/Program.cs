using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Xnk.Web;
using Xnk.Web.Services;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ⚠️ Singleton, KHÔNG phải Scoped — và đây là một cái bẫy đã đạp phải một lần.
//
// IHttpClientFactory dựng chuỗi message handler trong một scope RIÊNG của nó, tách khỏi
// scope mà component đang chạy. Với đăng ký Scoped, TokenHandler nhận về một PhienDangNhap
// KHÁC với cái mà trang đăng nhập vừa lưu token vào — kết quả: đăng nhập báo thành công,
// header hiện đúng email, rồi mọi lời gọi API trả 401 "phiên đã hết hạn".
//
// Trong Blazor WebAssembly, mỗi phiên bản ứng dụng phục vụ đúng một người dùng, nên
// Singleton là đúng ngữ nghĩa chứ không phải một mẹo đi vòng.
builder.Services.AddSingleton<PhienDangNhap>();
builder.Services.AddSingleton<TokenHandler>();

// BaseAddress là chính origin đang phục vụ trang này. nginx (local) và CloudFront (cloud)
// đều đưa cả giao diện lẫn API về cùng một origin, nên không có CORS và không có địa chỉ
// nào phải cấu hình theo môi trường.
builder.Services.AddHttpClient<XnkApiClient>(client =>
    {
        client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);

        // ⚠️ Không đặt Timeout thì mặc định của HttpClient là 100 giây, nhưng trong Blazor
        // WASM lời gọi đi qua fetch của trình duyệt và mặc định đó KHÔNG áp — thực tế là
        // chờ vô hạn.
        //
        // `chat` cho generation 120 giây (DownstreamOptions.GenerationTimeoutSeconds), nên
        // trần của giao diện phải RỘNG HƠN con số đó: hẹp hơn thì giao diện bỏ cuộc trong
        // khi backend vẫn đang làm việc, và người dùng thấy một lỗi cho một câu trả lời
        // sắp có. Rộng hơn một chút để lỗi thật hiện ra là "backend trả 502", đúng nơi có
        // thông tin, thay vì "Đang tải…" đứng mãi cho tới khi người dùng tự tải lại trang.
        client.Timeout = TimeSpan.FromSeconds(150);
    })
    .AddHttpMessageHandler<TokenHandler>();

await builder.Build().RunAsync();
