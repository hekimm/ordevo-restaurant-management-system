import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  RefreshControl,
  ActivityIndicator,
  Dimensions,
} from 'react-native';
import { BlurView } from 'expo-blur';
import { LinearGradient } from 'expo-linear-gradient';
import { Ionicons } from '@expo/vector-icons';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { RootStackParamList } from '../types';
import { supabase, RestaurantTable, Order } from '../lib/supabase';

type Props = NativeStackScreenProps<RootStackParamList, 'Tables'>;

const { width, height } = Dimensions.get('window');

interface TableWithOrder extends RestaurantTable {
  activeOrder?: Order & { itemCount?: number; elapsedTime?: string };
}

export default function TablesScreen({ navigation }: Props) {
  const [tables, setTables] = useState<TableWithOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const getElapsedTime = (openedAt: string): string => {
    const now = new Date();
    const opened = new Date(openedAt);
    const diffMs = now.getTime() - opened.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    
    if (diffMins < 60) return `${diffMins}dk`;
    const hours = Math.floor(diffMins / 60);
    const mins = diffMins % 60;
    return `${hours}s ${mins}dk`;
  };

  const loadTables = async () => {
    try {
      const { data: tablesData, error: tablesError } = await supabase
        .from('restaurant_tables')
        .select('*')
        .eq('is_active', true)
        .order('sort_order');

      if (tablesError) throw tablesError;

      const { data: ordersData } = await supabase
        .from('orders')
        .select('id, table_id, opened_at')
        .eq('status', 'open');

      const ordersMap = new Map();

      if (ordersData && ordersData.length > 0) {
        for (const order of ordersData) {
          const { count } = await supabase
            .from('order_items')
            .select('*', { count: 'exact', head: true })
            .eq('order_id', order.id)
            .neq('status', 'cancelled');

          if (count && count > 0) {
            ordersMap.set(order.table_id, {
              ...order,
              itemCount: count,
              elapsedTime: getElapsedTime(order.opened_at),
            });
          }
        }
      }

      const tablesWithOrders = (tablesData || []).map((table) => ({
        ...table,
        activeOrder: ordersMap.get(table.id),
      }));

      setTables(tablesWithOrders);
    } catch (error: any) {
      console.error('Masalar yüklenirken hata:', error.message);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    loadTables();

    // Realtime subscription - orders ve order_items değişikliklerini dinle
    console.log('Tables realtime subscription başlatılıyor...');
    const channel = supabase
      .channel('tables-realtime')
      .on(
        'postgres_changes',
        {
          event: '*',
          schema: 'public',
          table: 'orders',
        },
        (payload) => {
          console.log('Orders değişikliği:', payload);
          loadTables();
        }
      )
      .on(
        'postgres_changes',
        {
          event: '*',
          schema: 'public',
          table: 'order_items',
        },
        (payload) => {
          console.log('Order items değişikliği:', payload);
          loadTables();
        }
      )
      .on(
        'postgres_changes',
        {
          event: '*',
          schema: 'public',
          table: 'restaurant_tables',
        },
        (payload) => {
          console.log('Tables değişikliği:', payload);
          loadTables();
        }
      )
      .subscribe((status) => {
        console.log('Tables realtime subscription durumu:', status);
      });

    // Her dakika da güncelle (yedek)
    const interval = setInterval(loadTables, 60000);

    return () => {
      console.log('Tables realtime subscription kapatılıyor...');
      channel.unsubscribe();
      clearInterval(interval);
    };
  }, []);

  const onRefresh = () => {
    setRefreshing(true);
    loadTables();
  };

  const renderTable = ({ item }: { item: TableWithOrder }) => {
    const hasOrder = !!item.activeOrder;

    return (
      <TouchableOpacity
        style={styles.tableCardContainer}
        onPress={() => {
          if (hasOrder) {
            navigation.navigate('OrderDetail', { orderId: item.activeOrder!.id });
          } else {
            navigation.navigate('CreateOrder', { tableId: item.id });
          }
        }}
        activeOpacity={0.85}
      >
        <BlurView 
          intensity={35} 
          tint="light" 
          style={[
            styles.tableCard,
            hasOrder ? styles.tableCardBusy : styles.tableCardEmpty
          ]}
        >
          {/* Inner Glow */}
          <LinearGradient
            colors={
              hasOrder
                ? ['rgba(246, 198, 103, 0.35)', 'rgba(246, 198, 103, 0.08)', 'rgba(246, 198, 103, 0.25)']
                : ['rgba(74, 222, 128, 0.25)', 'rgba(74, 222, 128, 0.05)', 'rgba(74, 222, 128, 0.15)']
            }
            locations={[0, 0.5, 1]}
            style={styles.cardInnerGlow}
          />

          {/* Outer Glow */}
          <View 
            style={[
              styles.cardOuterGlow,
              hasOrder ? styles.goldGlow : styles.greenGlow
            ]} 
          />

          <View style={styles.cardContent}>
            {/* Table Name */}
            <Text style={[styles.tableName, hasOrder && styles.tableNameBusy]}>
              {item.name}
            </Text>

            {/* Status & Time */}
            {hasOrder && item.activeOrder?.elapsedTime ? (
              <View style={styles.statusContainer}>
                <Text style={styles.clockIcon}>🕐</Text>
                <Text style={styles.timeText}>
                  {item.activeOrder.elapsedTime}
                </Text>
              </View>
            ) : (
              <View style={styles.statusContainer}>
                <Text style={styles.emptyText}>Boş</Text>
              </View>
            )}

            {/* Item Count */}
            {hasOrder && item.activeOrder?.itemCount && (
              <View style={styles.itemBadge}>
                <Text style={styles.itemBadgeText}>
                  {item.activeOrder.itemCount} ürün
                </Text>
              </View>
            )}
          </View>

          {/* Refraction Bloom */}
          <View style={styles.refractionBloom} />
        </BlurView>
      </TouchableOpacity>
    );
  };

  if (loading) {
    return (
      <View style={styles.centerContainer}>
        <ActivityIndicator size="large" color="#D4AF37" />
        <Text style={styles.loadingText}>Masalar yükleniyor...</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {/* Animated Gradient Background */}
      <LinearGradient
        colors={['#FAFAFA', '#F5F5F5', '#FFFFFF', '#F8F8F8', '#FAFAFA']}
        locations={[0, 0.25, 0.5, 0.75, 1]}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 1 }}
        style={StyleSheet.absoluteFill}
      />

      {/* Light Streaks */}
      <View style={styles.lightStreak1}>
        <LinearGradient
          colors={['rgba(240, 240, 245, 0.5)', 'transparent']}
          start={{ x: 0, y: 0 }}
          end={{ x: 1, y: 1 }}
          style={styles.streakGradient}
        />
      </View>

      <View style={styles.lightStreak2}>
        <LinearGradient
          colors={['rgba(245, 245, 250, 0.4)', 'transparent']}
          start={{ x: 1, y: 0 }}
          end={{ x: 0, y: 1 }}
          style={styles.streakGradient}
        />
      </View>

      <FlatList
        data={tables}
        renderItem={renderTable}
        keyExtractor={(item) => item.id}
        numColumns={2}
        contentContainerStyle={styles.listContainer}
        refreshControl={
          <RefreshControl 
            refreshing={refreshing} 
            onRefresh={onRefresh}
            tintColor="#007AFF"
          />
        }
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <BlurView intensity={30} tint="light" style={styles.emptyBlur}>
              <Text style={styles.emptyTextMain}>Henüz masa eklenmemiş</Text>
            </BlurView>
          </View>
        }
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  centerContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingText: {
    marginTop: 16,
    fontSize: 16,
    color: 'rgba(26, 26, 46, 0.6)',
    fontWeight: '500',
  },
  lightStreak1: {
    position: 'absolute',
    top: -height * 0.2,
    right: -width * 0.3,
    width: width * 1.2,
    height: height * 0.6,
    transform: [{ rotate: '25deg' }],
  },
  lightStreak2: {
    position: 'absolute',
    bottom: -height * 0.2,
    left: -width * 0.3,
    width: width * 1.2,
    height: height * 0.5,
    transform: [{ rotate: '-15deg' }],
  },
  streakGradient: {
    flex: 1,
  },
  listContainer: {
    padding: 16,
    paddingBottom: 32,
  },
  tableCardContainer: {
    flex: 1,
    margin: 8,
    maxWidth: '50%',
  },
  tableCard: {
    borderRadius: 32,
    overflow: 'hidden',
    minHeight: 160,
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 12 },
    shadowOpacity: 0.15,
    shadowRadius: 24,
    elevation: 12,
  },
  tableCardEmpty: {
    borderWidth: 2,
    borderColor: 'rgba(74, 222, 128, 0.8)',
  },
  tableCardBusy: {
    borderWidth: 2,
    borderColor: 'rgba(246, 198, 103, 0.9)',
  },
  cardInnerGlow: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    height: '60%',
    borderTopLeftRadius: 32,
    borderTopRightRadius: 32,
  },
  cardOuterGlow: {
    position: 'absolute',
    top: -3,
    left: -3,
    right: -3,
    bottom: -3,
    borderRadius: 34,
    zIndex: -1,
  },
  goldGlow: {
    backgroundColor: 'rgba(246, 198, 103, 0.4)',
    shadowColor: '#F6C667',
    shadowOffset: { width: 0, height: 0 },
    shadowOpacity: 0.7,
    shadowRadius: 24,
  },
  greenGlow: {
    backgroundColor: 'rgba(74, 222, 128, 0.35)',
    shadowColor: '#4ADE80',
    shadowOffset: { width: 0, height: 0 },
    shadowOpacity: 0.6,
    shadowRadius: 20,
  },
  refractionBloom: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    height: 30,
    backgroundColor: 'rgba(255, 255, 255, 0.05)',
    borderBottomLeftRadius: 32,
    borderBottomRightRadius: 32,
  },
  cardContent: {
    padding: 20,
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: 160,
  },
  tableName: {
    fontSize: 24,
    fontWeight: '700',
    color: '#1A1A2E',
    marginBottom: 12,
    letterSpacing: -0.5,
  },
  tableNameBusy: {
    color: '#1A1A2E',
  },
  statusContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 8,
  },
  clockIcon: {
    fontSize: 16,
    marginRight: 6,
  },
  timeText: {
    fontSize: 15,
    fontWeight: '600',
    color: '#1A1A2E',
    letterSpacing: 0.3,
  },
  emptyText: {
    fontSize: 15,
    fontWeight: '600',
    color: '#4ADE80',
    letterSpacing: 0.3,
  },
  itemBadge: {
    marginTop: 8,
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 12,
    backgroundColor: 'rgba(246, 198, 103, 0.15)',
  },
  itemBadgeText: {
    fontSize: 13,
    fontWeight: '600',
    color: '#1A1A2E',
  },
  emptyContainer: {
    flex: 1,
    padding: 60,
    alignItems: 'center',
    justifyContent: 'center',
  },
  emptyBlur: {
    padding: 32,
    borderRadius: 24,
    backgroundColor: 'rgba(255, 255, 255, 0.12)',
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.25)',
  },
  emptyTextMain: {
    fontSize: 16,
    color: 'rgba(26, 26, 46, 0.6)',
    fontWeight: '500',
    textAlign: 'center',
  },
});
