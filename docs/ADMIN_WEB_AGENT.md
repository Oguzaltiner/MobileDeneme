# Admin Web Agent Sözleşmesi

`admin-web/` için kullanılan uzman rolü `admin-web-worker` olarak tanımlanır. Bu rol React/Vite arayüzünü, API sözleşmelerini ve içerik operasyonu UX’ini geliştirir.

## Sorumluluklar

- Admin login ve JWT oturumunu yönetmek; token’ı URL’ye veya loglara yazmamak.
- `overview`, vocabulary review queue, publish/reject ve audit endpointlerini backend sözleşmesine uygun tüketmek.
- 401/403, loading, empty ve mutation error durumlarını görünür göstermek.
- Taslak → incelemede → yayınlandı/reddedildi akışını açık ve geri bildirimli tasarlamak.
- `admin-web` içinde `npm run typecheck` ve `npm run build` çalıştırmak.
- Local Vite origin `http://localhost:5173` için API CORS ayarı vardır; production’da `AdminWeb:Origin` deployed admin origin’e ayarlanmalıdır.

## Review kapısı

`admin-web-reviewer`, yalnızca okunabilir review yapar; özellikle yetki kontrolünü server-side bırakma, token sızıntısı, başarısız mutation sonrası yanlış başarı mesajı ve mobil/backend sözleşme uyumunu kontrol eder.

Bu dosya, workspace’in `.agents` klasörü salt-okunur olduğu için admin web rolünün repo içi çalışma sözleşmesidir.
