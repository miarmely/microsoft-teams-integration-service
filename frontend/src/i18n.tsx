import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";

export type Locale = "en" | "tr";
type I18nContextValue = { locale: Locale; setLocale: (locale: Locale) => void; t: (text: string) => string };
const STORAGE_KEY = "teams-integration.locale";

const tr: Record<string, string> = {
  "Teams": "Teams",
  "Connect account": "Hesap bağlantısı",
  "People": "Kişiler",
  "Channels": "Kanallar",
  "Messaging": "Mesajlaşma",
  "Message people": "Kişilere mesaj gönder",
  "Send channel card": "Kanala kart gönder",
  "Live messages": "Canlı mesajlar",
  "Synchronization": "Senkronizasyon",
  "Synchronized messages": "Senkronize mesajlar",
  "Sync messages": "Mesajları senkronize et",
  "Export messages": "Mesajları dışa aktar",
  "Delete messages": "Mesajları sil",
  "System": "Sistem",
  "Application logs": "Uygulama günlükleri",
  "Sign out": "Çıkış yap",
  "API workspace": "API çalışma alanı",
  "Microsoft Teams operations": "Microsoft Teams işlemleri",
  "Production-ready": "Üretime hazır",
  "Welcome back": "Tekrar hoş geldiniz",
  "Sign in to your workspace": "Çalışma alanınıza giriş yapın",
  "Use your corporate AccessHub credentials.": "Kurumsal AccessHub bilgilerinizle giriş yapın.",
  "Username": "Kullanıcı adı",
  "Password": "Parola",
  "Sign in": "Giriş yap",
  "Sign in securely": "Güvenli giriş yap",
  "Signing in...": "Giriş yapılıyor...",
  "Enter your password": "Parolanızı girin",
  "Toggle password visibility": "Parola görünürlüğünü değiştir",
  "Your Teams data,": "Teams verileriniz,",
  "under control.": "kontrolünüz altında.",
  "Synchronize, inspect, and send channel messages from one protected enterprise workspace.": "Kanal mesajlarını tek bir korumalı kurumsal çalışma alanından senkronize edin, inceleyin ve gönderin.",
  "Permission-aware access and secure API communication": "İzin duyarlı erişim ve güvenli API iletişimi",
  "Your credentials are sent directly to your organization’s authentication service.": "Kimlik bilgileriniz doğrudan kuruluşunuzun kimlik doğrulama servisine gönderilir.",
  "AccessHub protected": "AccessHub korumalı",
  "Secure operations workspace": "Güvenli operasyon çalışma alanı",
  "Enterprise Integration Platform": "Kurumsal Entegrasyon Platformu",
  "Controlled data pipeline": "Kontrollü veri hattı",
  "Microsoft Entra directory": "Microsoft Entra dizini",
  "People, at a glance.": "Tüm kişiler, tek bakışta.",
  "Explore the people available to your connected Teams account and find the right person without leaving your workspace.": "Bağlı Teams hesabınızdaki kişileri keşfedin ve çalışma alanınızdan ayrılmadan doğru kişiyi bulun.",
  "Refresh directory": "Dizini yenile",
  "Directory size": "Dizin büyüklüğü",
  "Visible accounts": "Görünür hesaplar",
  "Mail enabled": "E-posta etkin",
  "Primary email available": "Birincil e-posta mevcut",
  "Principal IDs": "Asıl kimlikler",
  "Entra sign-in identities": "Entra oturum kimlikleri",
  "All people": "Tüm kişiler",
  "Active": "Aktif",
  "EMAIL": "E-POSTA",
  "USER PRINCIPAL NAME": "KULLANICI ASIL ADI",
  "Not available": "Mevcut değil",
  "Microsoft Teams user": "Microsoft Teams kullanıcısı",
  "Bringing your directory together": "Dizininiz hazırlanıyor",
  "Fetching the latest people from Microsoft Graph...": "En güncel kişiler Microsoft Graph'tan alınıyor...",
  "No people found": "Kişi bulunamadı",
  "Try a different name, email address, or user ID.": "Farklı bir ad, e-posta adresi veya kullanıcı kimliği deneyin.",
  "Direct conversations": "Doğrudan görüşmeler",
  "Message your people": "Kişilerinize mesaj gönderin",
  "Choose one person or a group. Each recipient receives a private Teams conversation from your connected account.": "Bir kişi veya grup seçin. Her alıcı, bağlı hesabınızdan özel bir Teams görüşmesi alır.",
  "Secure direct delivery": "Güvenli doğrudan teslimat",
  "Message delivered": "Mesaj teslim edildi",
  "Delivery partially completed": "Teslimat kısmen tamamlandı",
  "New direct message": "Yeni doğrudan mesaj",
  "Start a 1:1 or send separately to multiple people.": "Bire bir görüşme başlatın veya birden fazla kişiye ayrı ayrı gönderin.",
  "Recipients": "Alıcılar",
  "Required": "Zorunlu",
  "Search people by name or email": "Ada veya e-postaya göre kişi arayın",
  "Select all": "Tümünü seç",
  "Message": "Mesaj",
  "Sent as a private Teams chat message": "Özel Teams sohbet mesajı olarak gönderilir",
  "1:1 conversation": "Bire bir görüşme",
  "No recipients": "Alıcı yok",
  "Send message": "Mesaj gönder",
  "Sending...": "Gönderiliyor...",
  "Delivery list": "Teslimat listesi",
  "Nobody selected yet": "Henüz kimse seçilmedi",
  "Use the people picker to build your delivery list.": "Teslimat listenizi oluşturmak için kişi seçiciyi kullanın.",
  "Directory connected": "Dizin bağlı",
  "Refresh": "Yenile",
  "Loading your directory...": "Dizininiz yükleniyor...",
  "No matching people found.": "Eşleşen kişi bulunamadı.",
  "Microsoft Graph directory": "Microsoft Graph dizini",
  "View every accessible team and copy its Microsoft Graph identifier.": "Erişebildiğiniz tüm ekipleri görüntüleyin ve Microsoft Graph kimliklerini kopyalayın.",
  "Live directory": "Canlı dizin",
  "Search teams...": "Ekiplerde ara...",
  "No teams found": "Ekip bulunamadı",
  "Try another search term.": "Başka bir arama terimi deneyin.",
  "Loading teams...": "Ekipler yükleniyor...",
  "Select a team to view all of its channels and copy their identifiers.": "Kanallarını görüntülemek ve kimliklerini kopyalamak için bir ekip seçin.",
  "Select a team": "Bir ekip seçin",
  "Search channels...": "Kanallarda ara...",
  "Channel": "Kanal",
  "Type": "Tür",
  "Description": "Açıklama",
  "Channel ID": "Kanal kimliği",
  "No channels found": "Kanal bulunamadı",
  "This team has no matching channels.": "Bu ekipte eşleşen kanal yok.",
  "Its channels will be loaded on demand.": "Kanalları istendiğinde yüklenecektir.",
  "Loading channels...": "Kanallar yükleniyor...",
  "Select a channel": "Bir kanal seçin",
  "Microsoft Graph delivery": "Microsoft Graph teslimatı",
  "Send a hosted-image card": "Barındırılan görselli kart gönder",
  "Send an Adaptive Card as the connected Microsoft Teams user. Image bytes are stored as Teams hosted content.": "Bağlı Microsoft Teams kullanıcısı olarak bir Uyarlanabilir Kart gönderin. Görseller Teams barındırılan içeriği olarak saklanır.",
  "Message title": "Mesaj başlığı",
  "Optional": "İsteğe bağlı",
  "Hosted images": "Barındırılan görseller",
  "Images are embedded as Teams hosted content.": "Görseller Teams barındırılan içeriğine gömülür.",
  "Synchronize a channel": "Bir kanalı senkronize et",
  "Copy Teams messages and hosted media into PostgreSQL and MinIO.": "Teams mesajlarını ve barındırılan medyayı PostgreSQL ile MinIO'ya kopyalayın.",
  "Select a team and channel": "Ekip ve kanal seçin",
  "From date": "Başlangıç tarihi",
  "To date": "Bitiş tarihi",
  "Synchronization complete": "Senkronizasyon tamamlandı",
  "The From date cannot be later than the To date.": "Başlangıç tarihi bitiş tarihinden sonra olamaz.",
  "Export synchronized messages": "Senkronize mesajları dışa aktar",
  "Download a channel report with its message dataset and stored images.": "Mesaj veri kümesi ve saklanan görselleri içeren kanal raporunu indirin.",
  "Export configuration": "Dışa aktarma ayarları",
  "Choose the synchronized channel and reporting period.": "Senkronize kanalı ve raporlama dönemini seçin.",
  "ZIP archive": "ZIP arşivi",
  "Archive contents": "Arşiv içeriği",
  "Protected export": "Korumalı dışa aktarma",
  "Uses your authenticated dashboard session": "Kimliği doğrulanmış panel oturumunuzu kullanır",
  "Delete synchronized messages": "Senkronize mesajları sil",
  "Remove a selected channel's synchronized records and stored media.": "Seçili kanalın senkronize kayıtlarını ve saklanan medyasını kaldırın.",
  "Permanent deletion": "Kalıcı silme",
  "Deletion scope": "Silme kapsamı",
  "Select exactly which stored channel history should be removed.": "Kaldırılacak kanal geçmişini tam olarak seçin.",
  "Before you continue": "Devam etmeden önce",
  "Only synchronized data is affected. Messages in Microsoft Teams are not deleted.": "Yalnızca senkronize veriler etkilenir. Microsoft Teams mesajları silinmez.",
  "I understand this operation is permanent.": "Bu işlemin kalıcı olduğunu anlıyorum.",
  "Permanent operation": "Kalıcı işlem",
  "System observability": "Sistem gözlemlenebilirliği",
  "Inspect service activity, follow request traces, and investigate failures.": "Servis etkinliğini inceleyin, istek izlerini takip edin ve hataları araştırın.",
  "Loading telemetry…": "Telemetri yükleniyor…",
  "Total events": "Toplam olay",
  "Errors on this page": "Bu sayfadaki hatalar",
  "Warnings on this page": "Bu sayfadaki uyarılar",
  "Active instances": "Aktif örnekler",
  "No matching events": "Eşleşen olay yok",
  "Try another level or search phrase on this page.": "Başka bir seviye veya arama ifadesi deneyin.",
  "Category": "Kategori",
  "Runtime": "Çalışma zamanı",
  "Trace ID": "İz kimliği",
  "Structured properties": "Yapılandırılmış özellikler",
  "Previous page": "Önceki sayfa",
  "Next page": "Sonraki sayfa",
  "Page size": "Sayfa boyutu",
  "No messages found": "Mesaj bulunamadı",
  "Your messages will appear here after you fetch them.": "Mesajlarınız getirildikten sonra burada görünür.",
  "Try another channel, date range, or search term.": "Başka bir kanal, tarih aralığı veya arama terimi deneyin.",
  "Connect Teams to use this page": "Bu sayfayı kullanmak için Teams'e bağlanın",
  "Microsoft Teams connection required": "Microsoft Teams bağlantısı gerekli",
  "Connect to Microsoft Teams": "Microsoft Teams'e bağlan",
  "Teams connection": "Teams bağlantısı",
  "Connection status": "Bağlantı durumu",
  "Microsoft Graph account": "Microsoft Graph hesabı",
  "Manage the work account used for Teams directory and message operations.": "Teams dizini ve mesaj işlemlerinde kullanılan iş hesabını yönetin.",
  "Disconnect": "Bağlantıyı kes",
  "Connected": "Bağlı",
  "Not connected": "Bağlı değil",
  "Copy": "Kopyala",
  "Copied": "Kopyalandı",
  "Search name, email or ID...": "Ad, e-posta veya kimlik ara...",
  "Type a name or email...": "Bir ad veya e-posta yazın...",
  "Search message, path, trace…": "Mesaj, yol veya iz ara…",
  "Write a clear, thoughtful message...": "Açık ve özenli bir mesaj yazın...",
  "Write the card description.": "Kart açıklamasını yazın.",
};

const patterns: Array<[RegExp, (...matches: string[]) => string]> = [
  [/^(\d+) people$/, (count) => `${count} kişi`],
  [/^(\d+) teams$/, (count) => `${count} ekip`],
  [/^(\d+) channels$/, (count) => `${count} kanal`],
  [/^(\d+) directory members$/, (count) => `${count} dizin üyesi`],
  [/^(\d+) of (\d+) people$/, (shown, total) => `${total} kişiden ${shown} tanesi`],
  [/^(\d+) selected$/, (count) => `${count} seçili`],
  [/^Send to (\d+) people$/, (count) => `${count} kişiye gönder`],
  [/^(\d+) recipients$/, (count) => `${count} alıcı`],
  [/^(\d+) people available$/, (count) => `${count} kişi mevcut`],
  [/^\+(\d+) more$/, (count) => `+${count} daha`],
  [/^Showing page (\d+) of (\d+)$/, (page, total) => `${total} sayfadan ${page}. sayfa`],
];

function translate(text: string) {
  if (tr[text]) return tr[text];
  for (const [pattern, replacement] of patterns) {
    const match = text.match(pattern);
    if (match) return replacement(...match.slice(1));
  }
  return text;
}

const I18nContext = createContext<I18nContextValue | null>(null);

export function LanguageProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>(() =>
    localStorage.getItem(STORAGE_KEY) === "tr" ? "tr" : "en",
  );
  const setLocale = (next: Locale) => {
    localStorage.setItem(STORAGE_KEY, next);
    setLocaleState(next);
  };
  const value = useMemo(() => ({ locale, setLocale, t: (text: string) => locale === "tr" ? translate(text) : text }), [locale]);
  useEffect(() => { document.documentElement.lang = locale; }, [locale]);
  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

export function useI18n() {
  const context = useContext(I18nContext);
  if (!context) throw new Error("useI18n must be used within LanguageProvider");
  return context;
}

type TextTranslationState = { source: string; rendered: string };
const textState = new WeakMap<Text, TextTranslationState>();
const translatedAttributes = ["placeholder", "title", "aria-label"] as const;

/** Translates legacy page copy while keeping React-owned content reversible. */
export function LocalizedInterface({ children }: { children: ReactNode }) {
  const { locale } = useI18n();
  useEffect(() => {
    let changing = false;
    const localize = (root: Node) => {
      changing = true;
      const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
      let node: Node | null = root.nodeType === Node.TEXT_NODE ? root : walker.nextNode();
      while (node) {
        const textNode = node as Text;
        const current = textNode.data;
        const prior = textState.get(textNode);
        const source = prior && (current === prior.rendered || current === prior.source) ? prior.source : current;
        const trimmed = source.trim();
        const rendered = locale === "tr" && trimmed ? source.replace(trimmed, translate(trimmed)) : source;
        textState.set(textNode, { source, rendered });
        if (current !== rendered) textNode.data = rendered;
        node = walker.nextNode();
      }
      const elements = root instanceof Element ? [root, ...root.querySelectorAll("*")] : [];
      elements.forEach((element) => translatedAttributes.forEach((attribute) => {
        const current = element.getAttribute(attribute);
        if (!current) return;
        const key = `i18n${attribute.replace("-", "")}`;
        const stored = (element as HTMLElement).dataset[key];
        const source = stored && (current === stored || current === translate(stored)) ? stored : current;
        (element as HTMLElement).dataset[key] = source;
        const rendered = locale === "tr" ? translate(source) : source;
        if (current !== rendered) element.setAttribute(attribute, rendered);
      }));
      changing = false;
    };
    localize(document.body);
    const observer = new MutationObserver((mutations) => {
      if (changing) return;
      mutations.forEach((mutation) => {
        if (mutation.type === "characterData" || mutation.type === "attributes") {
          localize(mutation.target);
        } else {
          mutation.addedNodes.forEach(localize);
        }
      });
    });
    observer.observe(document.body, {
      childList: true,
      subtree: true,
      characterData: true,
      attributes: true,
      attributeFilter: [...translatedAttributes],
    });
    return () => observer.disconnect();
  }, [locale]);
  return children;
}
