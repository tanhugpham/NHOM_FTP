using FileTransfer.Client.Networking;
using FileTransfer.Shared.DTOs;
using FileTransfer.Shared.Enums;
using FileTransfer.Shared.Helpers;
using FileTransfer.Shared.Protocols;
using FileTransfer.Shared.Responses;

using System;
using Forms = System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace FileTransfer.Client
{
    public partial class RequestsWindow : Window, INotifyPropertyChanged
    {
        private TcpClientService _clientService;
        private List<OfferViewModel> _offers;
        private MainWindow _mainWindow;
        private string _currentUsername;

        public RequestsWindow(TcpClientService clientService, List<OfferViewModel> offers, MainWindow mainWindow, string username)
        {
            InitializeComponent();
            _clientService = clientService;
            _offers = offers;
            _mainWindow = mainWindow;
            _currentUsername = username;

            this.DataContext = this;
            RefreshGrid();
        }

        private int _pendingCount;
        public int PendingCount
        {
            get => _pendingCount;
            set { _pendingCount = value; OnPropertyChanged(); }
        }

        private int _acceptedCount;
        public int AcceptedCount
        {
            get => _acceptedCount;
            set { _acceptedCount = value; OnPropertyChanged(); }
        }

        private int _rejectedCount;
        public int RejectedCount
        {
            get => _rejectedCount;
            set { _rejectedCount = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Opens the context menu from the ⋮ button
        private void btnContextMenu_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        // Helper to get OfferViewModel from a ContextMenu MenuItem
        private OfferViewModel GetOfferFromMenuItem(RoutedEventArgs e)
        {
            var menuItem = e.OriginalSource as System.Windows.Controls.MenuItem;
            if (menuItem == null) return null;

            var contextMenu = menuItem.Parent as System.Windows.Controls.ContextMenu;
            if (contextMenu?.PlacementTarget is System.Windows.Controls.Button button)
            {
                return button.DataContext as OfferViewModel;
            }
            return null;
        }

        private async void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            var offer = GetOfferFromMenuItem(e);
            if (offer == null) return;

            try
            {
                string requestJson = JsonHelper.Serialize(
                    new NetworkMessage
                    {
                        Type = MessageType.PushAccept,
                        JsonBody = offer.OfferId
                    });

                string responseJson = await _clientService.SendMessageAsync(requestJson);
                var response = JsonHelper.Deserialize<BaseResponseDto>(responseJson);

                if (response.Success)
                {
                    try
                    {
                        var files = JsonHelper.Deserialize<List<ServerPushFileDto>>(response.Message);
                        if (files != null && files.Count > 0)
                        {
                            // Open FolderBrowserDialog once
                            using (var dialog = new Forms.FolderBrowserDialog())
                            {
                                dialog.Description = "Select destination folder for received files";
                                dialog.ShowNewFolderButton = true;

                                if (dialog.ShowDialog() == Forms.DialogResult.OK)
                                {
                                    string baseFolder = dialog.SelectedPath;

                                    // Create subfolder: Offer_<OfferId>
                                    string subfolder = offer.FromUser + "_" + offer.OfferId;
                                    string savePath = Path.Combine(baseFolder, subfolder);

                                    // Avoid overwriting existing folder
                                    int suffix = 1;
                                    while (Directory.Exists(savePath))
                                    {
                                        savePath = Path.Combine(baseFolder, subfolder + "_" + suffix);
                                        suffix++;
                                    }

                                    Directory.CreateDirectory(savePath);

                                    // Save all files into the folder
                                    foreach (var f in files)
                                    {
                                        string filePath = Path.Combine(savePath, f.FileName);
                                        File.WriteAllBytes(filePath, f.FileData);
                                        _mainWindow.AddLog("Saved: " + filePath);
                                    }

                                    string msg = "📁 **Files Saved Successfully**\n\n" 
                                        + "• **" + files.Count + " file(s) received**\n"
                                        + "• **Location:** " + savePath;
                                    System.Windows.MessageBox.Show(msg, "Download Complete", 
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                        }
                    }
                    catch { }

                    _mainWindow.AddLog("Push offer accepted: " + offer.OfferId);

                    offer.IsDownloaded = true;
                    offer.StatusDisplay = "✅ Downloaded";
                    offer.OriginalOffer.Status = "Accepted";
                    RefreshGrid();
                }
            }
            catch (Exception ex)
            {
                _mainWindow.AddLog("Accept offer error: " + ex.Message);
            }
        }

        private async void btnReject_Click(object sender, RoutedEventArgs e)
        {
            var offer = GetOfferFromMenuItem(e);
            if (offer == null) return;

            try
            {
                string requestJson = JsonHelper.Serialize(
                    new NetworkMessage
                    {
                        Type = MessageType.PushReject,
                        JsonBody = offer.OfferId
                    });

                await _clientService.SendMessageAsync(requestJson);
                _mainWindow.AddLog("Push offer rejected: " + offer.OfferId);

                offer.StatusDisplay = "❌ Rejected";
                offer.OriginalOffer.Status = "Rejected";
                _offers.Remove(offer);
                RefreshGrid();
            }
            catch (Exception ex)
            {
                _mainWindow.AddLog("Reject offer error: " + ex.Message);
            }
        }

        private void btnDetail_Click(object sender, RoutedEventArgs e)
        {
            var offer = GetOfferFromMenuItem(e);
            if (offer == null) return;

            string formatted = "📄 **Push Offer Details**\n"
                + "━━━━━━━━━━━━━━━━━━━━━━\n"
                + "🆔 **Offer ID:** " + offer.OfferId + "\n"
                + "👤 **From:** " + offer.FromUser + "\n"
                + "📅 **Received:** " + offer.ReceivedAtDisplay + "\n"
                + "📦 **Total Size:** " + offer.SizeDisplay + "\n"
                + "━━━━━━━━━━━━━━━━━━━━━━\n"
                + "📋 **Files (" + offer.FileCount + "):**\n";
            foreach (var f in offer.FileNames)
                formatted += "   📎 " + f + "\n";
            System.Windows.MessageBox.Show(formatted, "Offer Detail", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Refresh now fetches new data via CheckForPush
        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string requestJson = JsonHelper.Serialize(
                    new NetworkMessage
                    {
                        Type = MessageType.CheckForPush,
                        JsonBody = "{}"
                    });

                string responseJson = await _clientService.SendMessageAsync(requestJson);
                var response = JsonHelper.Deserialize<BaseResponseDto>(responseJson);

                if (response != null && response.Success)
                {
                    // Parse List<ServerPushOfferDto> (multi-offer response)
                    try
                    {
                        var offers = JsonHelper.Deserialize<List<ServerPushOfferDto>>(response.Message);
                        if (offers != null && offers.Count > 0)
                        {
                            int newCount = 0;
                            foreach (var offer in offers)
                            {
                                if (!_offers.Any(o => o.OfferId == offer.OfferId))
                                {
                                    var vm = new OfferViewModel
                                    {
                                        OfferId = offer.OfferId,
                                        FromUser = offer.FromUser,
                                        FileCount = offer.Files.Count,
                                        SizeDisplay = FormatFileSize(offer.TotalSize),
                                        ReceivedAt = DateTime.Now,
                                        ReceivedAtDisplay = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                        FileNames = offer.Files.Select(f => f.FileName + " (" + FormatFileSize(f.FileSize) + ")").ToList(),
                                        TotalSize = offer.TotalSize,
                                        OriginalOffer = offer
                                    };
                                    _offers.Add(vm);
                                    newCount++;
                                }
                            }
                            if (newCount > 0)
                            {
                                _mainWindow.AddLog("Refresh found " + newCount + " new offer(s)");
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _mainWindow.AddLog("Refresh error: " + ex.Message);
            }

            RefreshGrid();
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1048576) return (bytes / 1024.0).ToString("F1") + " KB";
            if (bytes < 1073741824) return (bytes / 1048576.0).ToString("F1") + " MB";
            return (bytes / 1073741824.0).ToString("F2") + " GB";
        }

        private void dgOffers_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var offer = e.Row.DataContext as OfferViewModel;
            if (offer != null && offer.IsDownloaded)
            {
                e.Row.Background = new SolidColorBrush(Color.FromRgb(240, 253, 244));
            }
            else
            {
                e.Row.Background = Brushes.White;
            }
        }

        private void RefreshGrid()
        {
            var activeOffers = _offers
                .Where(o => o.OriginalOffer.Status != "Rejected")
                .OrderByDescending(o => o.ReceivedAt)
                .ToList();

            dgOffers.ItemsSource = null;
            dgOffers.ItemsSource = activeOffers;

            PendingCount = _offers.Count(o => !o.IsDownloaded && o.OriginalOffer.Status != "Rejected");
            AcceptedCount = _offers.Count(o => o.IsDownloaded && o.OriginalOffer.Status == "Accepted");
            RejectedCount = _offers.Count(o => o.OriginalOffer.Status == "Rejected");

            // Also update main window button
            _mainWindow.UpdateRequestsButton();
        }
    }
}