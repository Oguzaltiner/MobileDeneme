import { useEffect, useMemo, useState } from 'react';
import { Platform, Pressable, Text, View } from 'react-native';
import { useIAP, type Purchase } from 'expo-iap';
import { api } from '../../lib/api';

const MONTHLY_ID = 'premium_monthly';
const YEARLY_ID = 'premium_yearly';
const packageCopy = [
  { id: MONTHLY_ID, title: 'Aylık Premium', price: '99 TL / ay', note: 'Esnek başlangıç' },
  { id: YEARLY_ID, title: 'Yıllık Premium', price: '699 TL / yıl', note: 'En avantajlı paket' },
];

export function PremiumScreen({ navigation }: { navigation: { goBack: () => void } }) {
  const [selected, setSelected] = useState(YEARLY_ID);
  const [message, setMessage] = useState('');
  const [purchasing, setPurchasing] = useState(false);
  const { connected, subscriptions, fetchProducts, requestPurchase, finishTransaction } = useIAP({
    onPurchaseSuccess: async (purchase: Purchase) => {
      if (!purchase.purchaseToken) { setMessage('Google Play satın alma anahtarı alınamadı.'); return; }
      try {
        setPurchasing(true);
        await api.verifyGooglePurchase({ productId: purchase.productId, purchaseToken: purchase.purchaseToken });
        await finishTransaction({ purchase, isConsumable: false });
        setMessage('Premium aktif edildi.');
      } catch (error) {
        setMessage((error as { message?: string }).message ?? 'Satın alma doğrulanamadı.');
      } finally { setPurchasing(false); }
    },
    onPurchaseError: (error) => setMessage(error.message ?? 'Google Play satın alma işlemi başarısız oldu.'),
    onError: (error) => setMessage(error.message),
  });
  useEffect(() => { if (connected) void fetchProducts({ skus: [MONTHLY_ID, YEARLY_ID], type: 'subs' }); }, [connected, fetchProducts]);
  const storePrices = useMemo(() => new Map(subscriptions.map((item) => [item.id, item.displayPrice])), [subscriptions]);
  const buy = async () => {
    setMessage(''); setPurchasing(true);
    try { await requestPurchase({ request: { google: { skus: [selected] }, apple: { sku: selected } }, type: 'subs' }); }
    catch (error) { setMessage((error as { message?: string }).message ?? 'Satın alma başlatılamadı.'); setPurchasing(false); }
  };
  return <View className="flex-1 bg-background px-6 pt-16"><Pressable onPress={navigation.goBack}><Text className="font-semibold text-blue-700">‹ Geri</Text></Pressable><Text className="mt-8 text-3xl font-bold text-foreground">Premium’a geç</Text><Text className="mt-2 text-muted-foreground">Daha fazla kelime, sınırsız quiz ve reklamsız öğrenme.</Text><View className="mt-8 gap-3">{packageCopy.map((item) => <Pressable key={item.id} onPress={() => setSelected(item.id)} className={`rounded-2xl border p-5 ${selected === item.id ? 'border-blue-600 bg-blue-50' : 'border-transparent bg-white'}`}><View className="flex-row items-center justify-between"><Text className="text-lg font-bold text-foreground">{item.title}</Text><Text className="font-bold text-blue-700">{storePrices.get(item.id) ?? item.price}</Text></View><Text className="mt-2 text-sm text-muted-foreground">{item.note}</Text></Pressable>)}</View><View className="mt-8 rounded-2xl bg-white p-5"><Text className="font-bold text-foreground">Premium ile açılanlar</Text><Text className="mt-3 leading-6 text-muted-foreground">A1–C2 seviyeleri · sınırsız quiz · listening/pronunciation · kişisel listeler · reklamsız kullanım</Text></View>{message ? <Text className="mt-5 text-center text-red-700">{message}</Text> : null}<Pressable disabled={purchasing || !connected || Platform.OS === 'web'} onPress={() => void buy()} className="mt-8 items-center rounded-2xl bg-amber-500 py-4"><Text className="font-bold text-white">{purchasing ? 'İşleniyor…' : !connected ? 'Google Play bağlanıyor…' : Platform.OS === 'web' ? 'Mobilde satın al' : 'Google Play ile satın al'}</Text></Pressable><Text className="mt-4 text-center text-xs text-muted-foreground">Satın alma Google Play Billing üzerinden yapılır. Test için development build ve Play Console ürünleri gerekir.</Text></View>;
}
