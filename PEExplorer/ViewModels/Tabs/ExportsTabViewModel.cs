﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using PEExplorer.Views;
using Prism.Commands;
using Zodiacon.PEParsing;
using Zodiacon.WPF;

namespace PEExplorer.ViewModels.Tabs {
	[Export, PartCreationPolicy(CreationPolicy.NonShared)]
	class ExportsTabViewModel : TabViewModelBase {
		[ImportingConstructor]
		public ExportsTabViewModel(MainViewModel mainViewModel) : base(mainViewModel) {
			DisassembleCommand = new DelegateCommand(() => {
				var symbol = SelectedItem;
				var vm = DialogService.CreateDialog<DisassemblyViewModel, DisassemblyView>(symbol.Name);
				var address = (int)symbol.Address;
                var parser = mainViewModel.Parser;

				parser.ReadArray(parser.RvaToFileOffset(address), _bytes);
				vm.Disassemble(_bytes, address, MainViewModel.Parser.IsPE64);
				vm.Show();
			}, () => SelectedItem != null && string.IsNullOrEmpty(SelectedItem.ForwardName)).ObservesProperty(() => SelectedItem);
		}

		public override string Icon => "/icons/export1.ico";

		public override string Text => "Exports";

		ICollection<ExportedSymbol> _exports;

		public unsafe ICollection<ExportedSymbol> Exports {
			get {
				if(_exports == null) {
					_exports = MainViewModel.Parser.GetExports();
				}
				return _exports;
			}
		}

		private string _searchText;

		public string SearchText {
			get { return _searchText; }
			set {
				if(SetProperty(ref _searchText, value)) {
					var view = CollectionViewSource.GetDefaultView(Exports);
					if(string.IsNullOrWhiteSpace(value))
						view.Filter = null;
					else {
						var lower = value.ToLower();
						view.Filter = o => {
							var symbol = (ExportedSymbol)o;
							return symbol.Name.ToLower().Contains(lower) || (symbol.ForwardName != null && symbol.ForwardName.ToLower().Contains(lower));
						};
					}
				}
			}
		}

		[Import]
		IDialogService DialogService;

		static byte[] _bytes = new byte[1 << 12];

		public ICommand DisassembleCommand { get; }

		private ExportedSymbol _selectedItem;

		public ExportedSymbol SelectedItem {
			get { return _selectedItem; }
			set { SetProperty(ref _selectedItem, value); }
		}

        public string StatusMessage => $"{Exports.Count} Functions";

    }
}
