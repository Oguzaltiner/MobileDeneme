import { useEffect, useMemo, useState } from 'react';
import { Platform, Pressable, ScrollView, Text, View } from 'react-native';
import { useIAP, type Purchase } from 'expo-iap';
import { api } from '../../lib/api';

const MONTHLY_ID = 'premium_monthly';
const YEARLY_ID = 'premium_yearly';
const packageCopy = [
  { id: MONTHLY_ID, title: 'Aylık Premium', price: '99 TL / ay', note: 'Esnek başlangıç', badge: '' },
  { id: YEARLY_ID, title: 'Yıllık Premium', price: '699 TL / yıl', note: 'Aylık ödemeye göre daha avantajlı', badge: 'ÖNERİLEN' },
];
type Notice = { kind: 'error' | 'success' | 'info'; text: string } | null;

export function PremiumScreen({ navigation }: { navigation: { goBack: () => void } }) {
  const [selected, setSelected] = useState(YEARLY_ID);
  const [notice, setNotice] = useState<Notice>(null);
  const [purchasing, setPurchasing] = useState(false);
  const [catalogLoading, setCatalogLoading] = useState(false);
  const [storeUnavailable, setStoreUnavailable] = useState(false);
  const { connected, subscriptions, fetchProducts, requestPurchase, finishTransaction } = useIAP({
    onPurchaseSuccess: async (purchase: Purchase) => {
      try {
        setPurchasing(true);
        if (!purchase.purchaseToken) {
          setNotice({ kind: 'error', text: 'Google Play anahtarı alınamadı. Development build ve Play test hesabını kontrol edin.' });
          return;
        }
        await api.verifyGooglePurchase({ productId: purchase.productId, purchaseToken: purchase.purchaseToken });
        await finishTransaction({ purchase, isConsumable: false });
        setNotice({ kind: 'success', text: 'Premium aktif edildi.' });
      } catch (error) {
        setNotice({ kind: 'error', text: (error as { message?: string }).message ?? 'Satın alma doğrulanamadı.' });
      } finally { setPurchasing(false); }
    },
    onPurchaseError: (error) => { setPurchasing(false); setNotice({ kind: 'error', text: error.message ?? 'Google Play satın alma işlemi başarısız oldu.' }); },
    onError: (error) => { setPurchasing(false); setNotice({ kind: 'error', text: error.message ?? 'Mağaza bağlantısı kurulamadı.' }); },
  });
  useEffect(() => {
    if (Platform.OS === 'web') return;
    if (!connected) { const timer = setTimeout(() => setStoreUnavailable(true), 8000); return () => clearTimeout(timer); }
    setStoreUnavailable(false); setCatalogLoading(true);
    void fetchProducts({ skus: [MONTHLY_ID, YEARLY_ID], type: 'subs' }).finally(() => setCatalogLoading(false));
  }, [connected, fetchProducts]);
  const storePrices = useMemo(() => new Map(subscriptions.map((item) => [item.id, item.displayPrice])), [subscriptions]);
  const isWeb = Platform.OS === 'web';
  const canPurchase = !isWeb && connected && !storeUnavailable && !purchasing;
  const buy = async () => {
    setNotice(null); setPurchasing(true);
    try { await requestPurchase({ request: { google: { skus: [selected] }, apple: { sku: selected } }, type: 'subs' }); }
    catch (error) { setNotice({ kind: 'error', text: (error as { message?: string }).message ?? 'Satın alma başlatılamadı.' }); setPurchasing(false); }
  };
  const buttonLabel = isWeb ? 'Mobil uygulamada satın al' : purchasing ? 'Satın alma işleniyor…' : catalogLoading ? 'Paketler yükleniyor…' : storeUnavailable ? 'Development build gerekli' : 'Google Play ile devam et';
  return <ScrollView className="flex-1 bg-background" contentContainerClassName="px-6 pb-10 pt-14">
    <Pressable onPress={navigation.goBack} className="self-start rounded-full bg-white px-4 py-2"><Text className="font-semibold text-blue-700">‹  Geri</Text></Pressable>
    <View className="mt-7"><Text className="text-4xl font-bold leading-tight text-foreground">Daha hızlı öğren.</Text><Text className="mt-2 text-base leading-6 text-muted-foreground">Premium ile tüm seviyeleri aç, tekrarlarını sınırsız yap ve reklamsız çalış.</Text></View>
    <View className="mt-6 rounded-3xl bg-blue-700 p-6"><Text className="text-sm font-bold uppercase tracking-widest text-blue-100">Premium</Text><Text className="mt-2 text-2xl font-bold text-white">Her gün daha güçlü kelime hafızası</Text><Text className="mt-2 leading-5 text-blue-100">A1’den C2’ye kadar tüm içerik ve kişisel tekrar planı.</Text></View>
    <View className="mt-6 gap-3">{packageCopy.map((item) => { const active = selected === item.id; return <Pressable key={item.id} onPress={() => setSelected(item.id)} className={`rounded-2xl border-2 p-5 ${active ? 'border-blue-600 bg-blue-50' : 'border-transparent bg-white'}`}><View className="flex-row items-start justify-between"><View className="flex-1 pr-3"><Text className="text-lg font-bold text-foreground">{item.title}</Text><Text className="mt-1 text-sm text-muted-foreground">{item.note}</Text></View><View className="items-end"><Text className="font-bold text-blue-700">{storePrices.get(item.id) ?? item.price}</Text>{item.badge ? <Text className="mt-2 rounded-full bg-amber-100 px-2 py-1 text-[10px] font-bold text-amber-700">{item.badge}</Text> : null}</View></View></Pressable>; })}</View>
    <View className="mt-6 rounded-2xl bg-white p-5"><Text className="font-bold text-foreground">Premium ile açılanlar</Text><Text className="mt-3 leading-6 text-muted-foreground">✓ A1–C2 seviyeleri{`\n`}✓ Sınırsız kelime ve quiz{`\n`}✓ Listening / pronunciation{`\n`}✓ Kişisel listeler ve reklamsız kullanım</Text></View>
    {notice ? <View className={`mt-5 rounded-2xl p-4 ${notice.kind === 'success' ? 'bg-emerald-50' : notice.kind === 'info' ? 'bg-blue-50' : 'bg-red-50'}`}><Text className={`text-center text-sm font-medium ${notice.kind === 'success' ? 'text-emerald-700' : notice.kind === 'info' ? 'text-blue-700' : 'text-red-700'}`}>{notice.text}</Text></View> : null}
    <Pressable disabled={!canPurchase} onPress={() => void buy()} className={`mt-6 items-center rounded-2xl py-4 ${canPurchase ? 'bg-amber-500' : 'bg-slate-300'}`}><Text className={`font-bold ${canPurchase ? 'text-white' : 'text-slate-600'}`}>{buttonLabel}</Text></Pressable>
    <Text className="mt-4 text-center text-xs leading-5 text-muted-foreground">Ödeme Google Play/App Store üzerinden alınır. Expo Go veya web önizlemesinde mağaza satın alımı kullanılamaz.</Text>
  </ScrollView>;
}
